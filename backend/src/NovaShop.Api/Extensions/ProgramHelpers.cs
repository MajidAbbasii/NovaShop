using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NovaShop.Domain.Entities;
using NovaShop.Infrastructure.Data;
using Scalar.AspNetCore;
using NovaShop.Api.Services;
using NovaShop.Api.Endpoints;
using Hangfire;
using NovaShop.Application.Jobs;
using NovaShop.Api.Middleware;
using NovaShop.Api.RateLimiting;
using NovaShop.Common.Models;
using Prometheus;

namespace NovaShop.Api.Extensions;

public static class ProgramHelpers
{
    public static void ConfigureLogging(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/novashop-.log", rollingInterval: Serilog.RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Consolidated service registrations
        builder.Services.AddNovaShopServices(builder.Configuration);

        // Host graceful shutdown
        builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));

        // Health checks
        var healthChecks = builder.Services.AddHealthChecks()
            .AddSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                name: "sql",
                failureStatus: HealthStatus.Unhealthy);

        var cacheSettings = builder.Configuration.GetSection("Cache").Get<CacheSettings>() ?? new CacheSettings();
        if (cacheSettings.Provider?.Equals("Redis", StringComparison.OrdinalIgnoreCase) == true &&
            !string.IsNullOrEmpty(cacheSettings.RedisConnectionString))
        {
            healthChecks.AddRedis(cacheSettings.RedisConnectionString, name: "redis", failureStatus: HealthStatus.Unhealthy);
        }

        // HttpClient
        builder.Services.AddHttpClient("default");

        // OpenAPI
        builder.Services.AddOpenApi();

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()        // برای توسعه
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Anti-forgery (required by form-bound minimal API endpoints)
        builder.Services.AddAntiforgery();
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        // Seed Data - Database Migration
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<NovaShopDbContext>();
            context.Database.Migrate();

            // Seed categories and products (idempotent)
            SeedData(services);
        }

        // OpenAPI and Scalar
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("NovaShop API")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }

        // Use custom exception handler
        app.UseCustomExceptionHandler();

        // Rate limiting — before auth so we catch unauthenticated requests too
        app.UseRateLimiting();

        // CORS
        app.UseCors();

        // Static files (uploaded images under wwwroot/images)
        app.UseStaticFiles();

        // Security
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Anti-forgery for form-based endpoints (image upload)
        app.UseAntiforgery();

        // Hangfire Dashboard — protected (Admin role or shared access key).
        var hangfireOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<HangfireOptions>>().Value;
        var hangfireLogger = app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AdminHangfireAuthorizationFilter>>();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new AdminHangfireAuthorizationFilter(hangfireOptions.DashboardAccessKey, hangfireLogger) },
            IgnoreAntiforgeryToken = true
        });

        // Global retry policy (configurable). Avoids infinite retries; back-off is
        // explicit when RetryDelaysInSeconds is provided, else Hangfire default.
        var retryAttempts = hangfireOptions.RetryAttempts;
        var retryDelays = hangfireOptions.RetryDelaysInSeconds is { Length: > 0 }
            ? hangfireOptions.RetryDelaysInSeconds
            : null;
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
        {
            Attempts = retryAttempts,
            DelaysInSeconds = retryDelays,
            OnAttemptsExceeded = AttemptsExceededAction.Delete
        });

        // Recurring job: release expired stock reservations every 5 minutes
        RecurringJob.AddOrUpdate<ReleaseExpiredReservationsJob>(
            "release-expired-reservations",
            job => job.ReleaseAllExpiredAsync(CancellationToken.None),
            "*/5 * * * *", // every 5 minutes
            queue: "critical");

        // Recurring job: rebuild full-text catalog daily at 3am
        RecurringJob.AddOrUpdate<RebuildFtsCatalogJob>(
            "rebuild-fts-catalog",
            job => job.RebuildAsync(CancellationToken.None),
            "0 3 * * *", // daily at 3am
            queue: "maintenance");

        // Recurring job: retry failed SMS notifications every 2 minutes
        RecurringJob.AddOrUpdate<RetryFailedNotificationsJob>(
            "retry-failed-notifications",
            job => job.RunAsync(CancellationToken.None),
            "*/2 * * * *", // every 2 minutes
            queue: "sms");

        // Recurring job: inventory health check every 30 minutes
        RecurringJob.AddOrUpdate<InventoryHealthCheckJob>(
            "inventory-health-check",
            job => job.RunAsync(CancellationToken.None),
            "*/30 * * * *", // every 30 minutes
            queue: "maintenance");

        // Recurring job: remind admins of aged custom-doll requests hourly
        RecurringJob.AddOrUpdate<CustomDollRequestReminderJob>(
            "custom-doll-request-reminder",
            job => job.RunAsync(CancellationToken.None),
            "0 * * * *", // hourly
            queue: "notifications");

        // Recurring job: payment reconciliation (no-op while online payments disabled)
        RecurringJob.AddOrUpdate<PaymentReconciliationJob>(
            "payment-reconciliation",
            job => job.RunAsync(CancellationToken.None),
            "*/15 * * * *", // every 15 minutes
            queue: "critical");

        // Map all endpoints
        MapEndpoints(app);
    }

    private static void SeedData(IServiceProvider services)
    {
        var context = services.GetRequiredService<NovaShopDbContext>();
        if (context.Categories.Any()) return;

        // Add categories
        context.Categories.AddRange(
            new Category { Name = "Animal Dolls", Description = "Adorable knitted animal dolls", ImageUrl = "https://picsum.photos/seed/animal/400/400" },
            new Category { Name = "Character Dolls", Description = "Lovable character dolls", ImageUrl = "https://picsum.photos/seed/character/400/400" },
            new Category { Name = "Baby Dolls", Description = "Soft baby-friendly dolls", ImageUrl = "https://picsum.photos/seed/baby/400/400" },
            new Category { Name = "Fantasy Dolls", Description = "Magical fantasy knitted dolls", ImageUrl = "https://picsum.photos/seed/fantasy/400/400" },
            new Category { Name = "Custom Handmade", Description = "Custom-made dolls to order", ImageUrl = "https://picsum.photos/seed/custom/400/400" }
        );
        context.SaveChanges();

        var animalCat = context.Categories.First(c => c.Name == "Animal Dolls");
        var characterCat = context.Categories.First(c => c.Name == "Character Dolls");
        var babyCat = context.Categories.First(c => c.Name == "Baby Dolls");
        var fantasyCat = context.Categories.First(c => c.Name == "Fantasy Dolls");

        context.Products.AddRange(
            new Product { Name = "Handmade Bunny Doll", Description = "A cute hand-knitted bunny doll with floppy ears and a sweet smile.", Price = 349_900m, Stock = 15, ImageUrl = "https://picsum.photos/seed/bunny/600/600", CategoryId = animalCat.Id, Rating = 4.8 },
            new Product { Name = "Cute Knitted Bear", Description = "A warm and huggable knitted teddy bear. Each bear is handcrafted with love using premium yarn.", Price = 425_000m, Stock = 10, ImageUrl = "https://picsum.photos/seed/bear/600/600", CategoryId = animalCat.Id, Rating = 4.9 },
            new Product { Name = "Little Fox Doll", Description = "An adorable little knitted fox with bright orange fur and a fluffy tail.", Price = 289_900m, Stock = 20, ImageUrl = "https://picsum.photos/seed/fox/600/600", CategoryId = animalCat.Id, Rating = 4.7 },
            new Product { Name = "Knitted Cat Doll", Description = "A charming hand-knitted cat with striped fur and big bright eyes.", Price = 320_000m, Stock = 12, ImageUrl = "https://picsum.photos/seed/kitten/600/600", CategoryId = animalCat.Id, Rating = 4.6 },
            new Product { Name = "Handmade Panda Doll", Description = "A cute knitted panda with black and white markings. Soft, cuddly, and made from eco-friendly materials.", Price = 389_900m, Stock = 8, ImageUrl = "https://picsum.photos/seed/panda/600/600", CategoryId = animalCat.Id, Rating = 4.9 },
            new Product { Name = "Small Elephant Doll", Description = "A sweet little knitted elephant with big floppy ears and a gentle expression.", Price = 265_000m, Stock = 18, ImageUrl = "https://picsum.photos/seed/elephant/600/600", CategoryId = animalCat.Id, Rating = 4.5 },
            new Product { Name = "Knitted Rabbit Girl", Description = "A beautiful knitted rabbit doll with a floral dress and braided yarn hair.", Price = 450_000m, Stock = 6, ImageUrl = "https://picsum.photos/seed/rabbitgirl/600/600", CategoryId = characterCat.Id, Rating = 5.0 },
            new Product { Name = "Handmade Teddy Bear", Description = "A classic handmade teddy bear in warm brown tones. Stuffed with hypoallergenic filling and dressed in a cozy scarf.", Price = 480_000m, Stock = 5, ImageUrl = "https://picsum.photos/seed/teddy/600/600", CategoryId = animalCat.Id, Rating = 4.8 },
            new Product { Name = "Crochet Unicorn Doll", Description = "A magical crocheted unicorn with a rainbow mane and golden horn. Handcrafted with sparkly yarn.", Price = 520_000m, Stock = 7, ImageUrl = "https://picsum.photos/seed/unicorn/600/600", CategoryId = fantasyCat.Id, Rating = 4.9 },
            new Product { Name = "Baby Penguin Doll", Description = "An adorable knitted penguin in a winter hat. Made with ultra-soft baby-safe yarn.", Price = 249_900m, Stock = 25, ImageUrl = "https://picsum.photos/seed/penguin/600/600", CategoryId = babyCat.Id, Rating = 4.7 }
        );
        context.SaveChanges();
    }

    private static void MapEndpoints(WebApplication app)
    {
        // Health endpoints
        app.MapHealthChecks("/health");

        // Prometheus metrics endpoint
        app.UseMetricServer();
        app.UseHttpMetrics();
        app.MapAuthEndpoints();
        app.MapProductsEndpoints();
        app.MapCategoriesEndpoints();
        app.MapReviewsEndpoints();
        app.MapCartEndpoints();
        app.MapOrdersEndpoints();
        app.MapPaymentsEndpoints();
        app.MapWalletEndpoints();
        app.MapNotificationsEndpoints();
        app.MapDiscountsEndpoints();
        app.MapUsersEndpoints();
        app.MapWishlistEndpoints();
        app.MapAdminEndpoints();
        app.MapImagesEndpoints();
        app.MapBannersEndpoints();
        app.MapCustomDollRequestsEndpoints();
        app.MapTranslationEndpoints();

    }
}

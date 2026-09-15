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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace NovaShop.Api.Extensions;

public static class ProgramHelpers
{
    public static void ConfigureLogging(WebApplicationBuilder builder)
    {
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        // File logging is only useful locally (Render's filesystem is ephemeral and
        // only captures stdout/stderr). The File sink is disabled in Production to
        // avoid wasting disk space and giving a false impression of log persistence.
        if (builder.Environment.IsDevelopment())
        {
            loggerConfig.WriteTo.File("logs/novashop-.log", rollingInterval: Serilog.RollingInterval.Day);
        }

        Log.Logger = loggerConfig.CreateLogger();

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
            .AddNpgSql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                name: "npgsql",
                failureStatus: HealthStatus.Unhealthy);

        var cacheSettings = builder.Configuration.GetSection("Cache").Get<CacheSettings>() ?? new CacheSettings();
        if (cacheSettings.Provider?.Equals("Redis", StringComparison.OrdinalIgnoreCase) == true &&
            !string.IsNullOrEmpty(cacheSettings.RedisConnectionString))
        {
            healthChecks.AddRedis(cacheSettings.RedisConnectionString, name: "redis", failureStatus: HealthStatus.Unhealthy);
        }

        // HttpClient
        builder.Services.AddHttpClient("default");

        // Reverse-proxy (Render / YARP) forwarded-header handling.
        // The app is only reachable through the proxy edge, so we trust forwarded headers
        // from the known proxy hop (loopback + private/CGNAT ranges) and reject them from
        // any directly-connected untrusted source. This makes Request.Scheme = https for
        // the original public request so UseHttpsRedirection() does NOT loop.
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            options.RequireHeaderSymmetry = false;
            options.ForwardLimit = null;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.1/8"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("100.64.0.0/10"));
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("169.254.0.0/16"));
        });

        // OpenAPI
        builder.Services.AddOpenApi();

        // CORS — restricted to known dev/prod origins (API is behind the gateway,
        // but we never use AllowAnyOrigin). Mirrors the gateway's origin policy.
        var apiCors = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var apiEnv = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var apiAllowed = apiCors.Concat(apiEnv)
            .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
            .Distinct().ToArray();
        var origins = apiAllowed.Length > 0
            ? apiAllowed
            : new[] { "http://localhost:3000", "http://localhost:3005" };

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Anti-forgery (required by form-bound minimal API endpoints)
        builder.Services.AddAntiforgery();

        // Emit Persian/Arabic text as readable UTF-8 JSON (not \uXXXX escapes)
        // across all minimal-API responses, while staying valid JSON.
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        // Honor reverse-proxy forwarded headers (Render terminates TLS at the edge).
        // MUST run before UseHttpsRedirection / UseCors / auth — it rewrites Scheme/Host
        // from X-Forwarded-Proto / X-Forwarded-Host so we don't infinitely redirect HTTP->HTTPS.
        app.UseForwardedHeaders();

        // Correlation ID — reads X-Correlation-ID from gateway (or generates one)
        // and pushes it to Serilog LogContext for all downstream log entries.
        app.UseCorrelationId();

        // Seed Data - Database Migration
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<NovaShopDbContext>();
            context.Database.Migrate();

            // Seed categories and products (idempotent)
            SeedData(services);

            // One-time Render database bootstrap (Translations + Users).
            // Disabled by default. Enable with env var: Render__SeedDatabase=true
            SeedRenderData(services, app.Configuration);
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

        // Serilog request-completion logging — logs one entry per HTTP request
        // (method, path, status code, duration). Active in all environments.
        app.UseSerilogRequestLogging();

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

        // PostgreSQL full-text search uses a STORED generated tsvector column on
        // Products (maintained automatically by the database on every INSERT/UPDATE).
        // No periodic FTS catalog rebuild job is required.

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

    // ---- One-time Render database bootstrap — REMOVE after initial deployment ----
    // When Render__SeedDatabase=true:
    //   1. Replaces ALL Translations from Seed/translations.json (delete + insert)
    //   2. Deletes ALL Users and reseeds from Seed/seed-users.json
    // Disabled by default. Remove after initial Render deployment is verified.
    private static void SeedRenderData(IServiceProvider services, IConfiguration config)
    {
        if (!config.GetValue<bool>("Render:SeedDatabase"))
            return;

        var context = services.GetRequiredService<NovaShopDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var contentRoot = services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var seedDir = Path.Combine(contentRoot, "Seed");

        var translationsPath = Path.Combine(seedDir, "translations.json");
        var usersPath = Path.Combine(seedDir, "seed-users.json");

        if (!File.Exists(translationsPath) || !File.Exists(usersPath))
        {
            logger.LogWarning("Render seed enabled but seed files not found at {Path}. Skipping.", seedDir);
            return;
        }

        // --- Load and validate seed data ---
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var translations = JsonSerializer.Deserialize<List<SeedTranslation>>(File.ReadAllText(translationsPath), jsonOptions);
        if (translations is null || translations.Count == 0)
        {
            logger.LogWarning("Render seed: translations.json is empty. Skipping.");
            return;
        }

        var users = JsonSerializer.Deserialize<List<SeedUser>>(File.ReadAllText(usersPath), jsonOptions);
        if (users is null || users.Count == 0)
        {
            logger.LogWarning("Render seed: seed-users.json is empty. Skipping.");
            return;
        }

        // Validate required fields
        var invalidTranslation = translations.FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Key) || string.IsNullOrWhiteSpace(t.Locale) || string.IsNullOrWhiteSpace(t.Value));
        if (invalidTranslation is not null)
        {
            logger.LogWarning("Render seed: translation record is missing required fields. Skipping.");
            return;
        }

        var invalidUser = users.FirstOrDefault(u => string.IsNullOrWhiteSpace(u.Username) || string.IsNullOrWhiteSpace(u.Email) || string.IsNullOrWhiteSpace(u.PasswordHash));
        if (invalidUser is not null)
        {
            logger.LogWarning("Render seed: user record is missing required fields. Skipping.");
            return;
        }

        logger.LogWarning("RENDER SEED: Destructive database bootstrap starting. Translations will be replaced. Users will be replaced.");

        // --- Execute in transaction ---
        using var transaction = context.Database.BeginTransaction();
        try
        {
            // 1. Replace ALL Translations: delete existing, then insert from seed file
            var deletedCount = context.Translations.ExecuteDelete();
            logger.LogWarning("RENDER SEED: Deleted {Count} existing translations", deletedCount);

            var entities = translations.Select(t => new Translation
            {
                Id = t.Id,
                Key = t.Key,
                Locale = t.Locale,
                Value = t.Value,
                Namespace = t.Namespace,
                Description = t.Description,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt.UtcDateTime,
                UpdatedAt = t.UpdatedAt.UtcDateTime,
                CreatedBy = t.CreatedBy,
                UpdatedBy = t.UpdatedBy,
            }).ToList();

            context.Translations.AddRange(entities);
            context.SaveChanges();

            // Reset sequence to MAX(Id)
            context.Database.ExecuteSqlRaw(
                "SELECT setval(pg_get_serial_sequence('\"Translations\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"Translations\"), 1))");

            logger.LogWarning("RENDER SEED: Inserted {Count} translations", entities.Count);

            // 2. Delete all Users and reseed
            var existingUsers = context.Users.ToList();
            context.Users.RemoveRange(existingUsers);
            context.SaveChanges();

            var userEntities = users.Select(u => new User
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                PasswordHash = u.PasswordHash,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                City = u.City,
                PostalCode = u.PostalCode,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt.UtcDateTime,
            }).ToList();

            context.Users.AddRange(userEntities);
            context.SaveChanges();

            // Reset sequence to MAX(Id)
            context.Database.ExecuteSqlRaw(
                "SELECT setval(pg_get_serial_sequence('\"Users\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"Users\"), 1))");

            logger.LogWarning("RENDER SEED: Users replaced: {Count}", userEntities.Count);

            transaction.Commit();

            logger.LogWarning("RENDER SEED: Complete. Translations: {TCount}, Users: {UCount}", translations.Count, users.Count);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "RENDER SEED: Failed. All changes rolled back.");
            throw;
        }
    }

    // ---- Temporary seed DTOs — REMOVE after initial deployment ----
    private sealed class SeedTranslation
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Locale { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Namespace { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    private sealed class SeedUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
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
        app.MapShippingEndpoints();
        // Temporary — REMOVE after migration: app.MapDatabaseAdminEndpoints();
        app.MapDatabaseAdminEndpoints();

    }
}

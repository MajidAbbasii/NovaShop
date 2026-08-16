using FluentValidation;
using Hangfire;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Data;
using NovaShop.Api.RateLimiting;
using NovaShop.Api.Services;
using NovaShop.Application.Behaviors;
using NovaShop.Application.Caching;
using NovaShop.Application.Features.Products.Queries;
using NovaShop.Application.Mappers;
using NovaShop.Application.Consumers;
using NovaShop.Application.Services;
using NovaShop.Common.Models;
using NovaShop.Domain.Repositories;
using NovaShop.Domain.Services;
using NovaShop.Infrastructure.Data;
using NovaShop.Infrastructure.Repositories;
using NovaShop.Infrastructure.Services;
using NovaShop.Application.Features.Products.Commands;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry;
using System.Text;
using Prometheus;

namespace NovaShop.Api.Extensions;

public static class NovaShopServiceCollectionExtensions
{
    public static IServiceCollection AddNovaShopServices(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureOptions(services, configuration);
        ConfigurePaymentPolicy(services, configuration);
        ConfigureAuthentication(services, configuration);
        ConfigureAuthorization(services);
        ConfigureOpenTelemetry(services);
        ConfigureHangfire(services, configuration);
        ConfigureMediatR(services);
        ConfigureRepositoriesAndMappers(services);
        ConfigureValidators(services);
        ConfigureCache(services, configuration);
        ConfigureDbContext(services, configuration);
        ConfigureMassTransit(services, configuration);
        ConfigurePaymentGateway(services);
        ConfigureSms(services, configuration);
        ConfigureRateLimiting(services, configuration);
        ConfigureImageServices(services, configuration);

        return services;
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<CacheSettings>(configuration.GetSection("Cache"));
        services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));
    }

    private static void ConfigurePaymentPolicy(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("PaymentPolicy")
                    .Get<PaymentPolicyOptions>() ?? new PaymentPolicyOptions();
        PaymentPolicy.Apply(options);
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();
        var key = jwtSettings?.Key ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it via User Secrets (dotnet user-secrets set \"Jwt:Key\" \"<strong-random-key>\") or an environment variable for Development/Production.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings?.Issuer ?? "NovaShop",
                    ValidAudience = jwtSettings?.Audience ?? "NovaShop",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                };
            });
    }

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                });
    }

    private static void ConfigureOpenTelemetry(IServiceCollection services)
    {
        services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation();
                    metrics.AddHttpClientInstrumentation();
                    metrics.AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation();
                    tracing.AddHttpClientInstrumentation();
                    tracing.AddSource("NovaShop");
                })
                .UseOtlpExporter();
    }

    private static void ConfigureHangfire(IServiceCollection services, IConfiguration configuration)
    {
        var hangfireOptions = configuration.GetSection("Hangfire").Get<HangfireOptions>() ?? new HangfireOptions();
        services.Configure<HangfireOptions>(configuration.GetSection("Hangfire"));
        services.Configure<JobsOptions>(configuration.GetSection("Jobs"));

        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"));
        });

        // Start a server listening on the configured queues (priority order matters:
        // "critical" is drained first). WorkerCount defaults to Hangfire when null.
        services.AddHangfireServer(options =>
        {
            options.Queues = hangfireOptions.Queues;
            if (hangfireOptions.WorkerCount is > 0)
                options.WorkerCount = hangfireOptions.WorkerCount.Value;
        });
    }

    private static void ConfigureMediatR(IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetProductsQuery).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    private static void ConfigureRepositoriesAndMappers(IServiceCollection services)
    {
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<ICartRepository, EfCartRepository>();
        services.AddScoped<IDiscountRepository, EfDiscountRepository>();
        services.AddScoped<IWishlistRepository, EfWishlistRepository>();

        // Essential order and cart item repository registrations for MediatR command handlers
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<ICartItemRepository, EfCartItemRepository>();

        // Password hashing (PBKDF2)
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // OTP store (in-memory; move to Redis for multi-instance)
        services.AddSingleton<OtpStore>();
        services.AddSingleton<PendingRegistrationStore>();

        // Register Mapperly mappers as singleton services
        services.AddSingleton<ProductMapper>();
        services.AddSingleton<CartMapper>();
        services.AddSingleton<CategoryMapper>();
        services.AddSingleton<ReviewMapper>();
        services.AddSingleton<UserMapper>();
        services.AddSingleton<OrderMapper>();
        services.AddSingleton<WishlistMapper>();
    }

    private static void ConfigureImageServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ImageStorageOptions>(configuration.GetSection("ImageStorage"));
        services.AddScoped<IImageStorage, LocalImageStorage>();
    }

    private static void ConfigureValidators(IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(CreateProductCommandValidator).Assembly);
    }

    private static void ConfigureCache(IServiceCollection services, IConfiguration configuration)
    {
        var cacheSettings = configuration.GetSection("Cache").Get<CacheSettings>() ?? new CacheSettings();

        if (cacheSettings.Provider?.Equals("Redis", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheSettings.RedisConnectionString ?? "localhost:6379";
                options.InstanceName = cacheSettings.InstanceName ?? "NovaShop_";
            });

            services.AddSingleton<ICacheService, DistributedCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
    }

    private static void ConfigureDbContext(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NovaShopDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Register IDbConnection for Dapper-based search
        services.AddScoped<IDbConnection>(sp =>
        {
            var connStr = configuration.GetConnectionString("DefaultConnection");
            return new Microsoft.Data.SqlClient.SqlConnection(connStr);
        });
    }

    private static void ConfigureMassTransit(IServiceCollection services, IConfiguration configuration)
    {
        var rabbit = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>();

        // In development, only register MassTransit if explicitly enabled (optional license)
        var enableMassTransit = configuration.GetValue<bool?>("Development:EnableMassTransit") ?? false;
        
        if (enableMassTransit)
        {
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbit.Host ?? "localhost", rabbit.VirtualHost ?? "/", h =>
                    {
                        h.Username(rabbit.Username ?? "guest");
                        h.Password(rabbit.Password ?? "guest");
                    });

                    cfg.ReceiveEndpoint(rabbit.ProductCreatedQueue ?? "product-created-queue", e =>
                    {
                        e.ConfigureConsumer<ProductCreatedConsumer>(context);
                    });

                    cfg.ReceiveEndpoint(rabbit.OrderEventsQueue ?? "order-events-queue", e =>
                    {
                        e.ConfigureConsumer<OrderCreatedConsumer>(context);
                        e.ConfigureConsumer<StockReservedConsumer>(context);
                    });
                });

                x.AddConsumer<ProductCreatedConsumer>();
                x.AddConsumer<OrderCreatedConsumer>();
                x.AddConsumer<StockReservedConsumer>();
            });

            // Register real IPublishEndpoint when MassTransit is enabled
            // Use native MassTransit IBus.Publish instead of IPublishEndpoint for MediatR integration
            // This follows MassTransit 9.x best practices where IPublishEndpoint is not required
        }
        else
        {
            // Provide stub implementation for IPublishEndpoint to avoid dependency issues
            services.AddSingleton<IPublishEndpoint, MassTransitPublishEndpointStub>();
        }

        // Note: IPublishEndpoint is no longer required for MediatR integration in MassTransit 9.x
        // MediatR will use native MassTransit publish mechanism via IBus.Publish
    }

    private static void ConfigurePaymentGateway(IServiceCollection services)
    {
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
                services.AddScoped<IWalletService, WalletService>();
                services.AddSingleton<MockPaymentStore>();
        services.AddScoped<IReservationScheduler, HangfireReservationScheduler>();
    }

    private static void ConfigureSms(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmsOptions>(configuration.GetSection("Sms"));

        // Provider chosen from config: "Log" | "Mock" | "Kavenegar" (real gateway)
        var provider = configuration.GetSection("Sms")["Provider"] ?? "Mock";
        services.AddHttpClient(nameof(KavenegarSmsService));
        services.AddSingleton<ISmsService>(sp => SmsServiceFactory.Create(sp, provider));

        services.AddScoped<INotificationService, NotificationService>();
    }

    private static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimit").Get<RateLimitSettings>() ?? new RateLimitSettings();
        services.Configure<RateLimitSettings>(configuration.GetSection("RateLimit"));

        if (settings.Enabled && settings.RedisConnectionString is { Length: > 0 })
        {
            // Redis-backed (distributed) store — already have IDistributedCache from ICacheService setup
            services.AddSingleton<IRateLimitCounterStore>(sp =>
            {
                var cache = sp.GetRequiredService<IDistributedCache>();
                return new RedisRateLimitCounterStore(cache, settings.InstanceName);
            });
        }
        else
        {
            // In-memory fallback
            services.AddSingleton<IRateLimitCounterStore, MemoryRateLimitCounterStore>();
        }
    }
}

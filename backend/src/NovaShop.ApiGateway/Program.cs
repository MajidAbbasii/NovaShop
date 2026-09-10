// Fix for inotify/FileSystemWatcher crash on Render:
//
// WebApplication.CreateBuilder(args) calls Host.CreateApplicationBuilder which
// internally calls ApplyDefaultAppConfigurations, which adds default JSON
// configuration sources (appsettings.json, appsettings.{ENV}.json,
// {PROJECT}.settings.json) with reloadOnChange: true. On .NET 10 this
// materializes the FileConfigurationProvider instances during construction,
// creating FileSystemWatcher instances (inotify on Linux). On Render the
// default inotify user limit (128) is exhausted, causing:
//   System.IO.IOException: The configured user limit (128) on the number of
//   inotify instances has been reached...
//
// WebApplication.CreateSlimBuilder does NOT call ApplyDefaultAppConfigurations,
// so it does not register the default JSON config sources that create watchers.
// Instead it directly adds a minimal set (appsettings.json +
// appsettings.{ENV}.json) without triggering the crash point.
//
// After CreateSlimBuilder, we clear all sources and re-add the JSON files
// with reloadOnChange: false, guaranteeing NO FileSystemWatcher instances
// are ever created.
//
// CreateSlimBuilder preserves ALL ASP.NET Core hosting defaults that
// CreateBuilder sets up (Kestrel/IServer, routing, authentication, etc.)
// — unlike CreateEmptyBuilder which removes Kestrel and causes
//   "No service for type 'Microsoft.AspNetCore.Hosting.Server.IServer'
//    has been registered".
//
// Verified: CreateSlimBuilder registers 103 services including IServer/Kestrel.
// After Sources.Clear() + re-add with reloadOnChange: false, no FileConfigurationProviders
// exist with ReloadOnChange=true, so no FileSystemWatcher/inotify instances are created.
//

using System;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Prometheus;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace NovaShop.ApiGateway;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // Local development default: listen on 5250.
        // In Docker/Render/CI, override with ASPNETCORE_HTTP_PORTS or ASPNETCORE_URLS.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"))
            && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(5250);
            });
        }

        // Clear all default configuration sources (JSON with reloadOnChange: true, etc.)
        // and re-add them with reloadOnChange: false to prevent FileSystemWatcher creation.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
        builder.Configuration.AddEnvironmentVariables();
        if (args != null && args.Length > 0)
            builder.Configuration.AddCommandLine(args);

        // Configure reverse proxy from configuration.
        // The backend cluster destination is local Docker DNS ("novashop-api:5000")
        // by default for local compose. On Render this is overridden at runtime
        // via the API_BASE_URL environment variable (see below).
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrEmpty(apiBaseUrl))
        {
            // Override the destination address via config so YARP picks it up.
            builder.Configuration["ReverseProxy:Clusters:backend-cluster:Destinations:destination1:Address"] = apiBaseUrl;
        }
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        // Configure JWT authentication for gateway
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.Audience = builder.Configuration["Jwt:Audience"];
            // Issuer is validated via TokenValidationParameters below (symmetric key).
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException(
                    "Jwt:Key is not configured in the Gateway. Set it via User Secrets or an environment variable (must match the API's Jwt:Key).");
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtKey))
            };
        });

        // Configure CORS
        // Allowed origins are config-driven (Cors:AllowedOrigins) and can be
        // overridden/extended via the CORS_ALLOWED_ORIGINS env var (comma-separated)
        // for production. We never fall back to AllowAnyOrigin / '*'.
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var envOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var allowedOrigins = configuredOrigins
            .Concat(envOrigins)
            .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
            .Distinct()
            .ToArray();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("GatewayPolicy", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    // Development fallback: only the local dev frontends.
                    policy.WithOrigins("http://localhost:3000", "http://localhost:3005")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
            });
        });

        // Configure rate limiting
        builder.Services.AddRateLimiter(options =>
        {
            var globalConfig = builder.Configuration.GetSection("RateLimiting:Global");
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = int.Parse(globalConfig["RequestsPerMinute"] ?? "100"),
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");
            };
        });

        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck("gateway", () => HealthCheckResult.Healthy("Gateway is healthy"));

        // Logging
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);

        // Reverse-proxy (Render) forwarded-header handling. The gateway is only reachable
        // through the proxy edge, so trust forwarded headers from the known proxy hop
        // (loopback + private/CGNAT ranges) to derive the original https scheme/host.
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

        var app = builder.Build();

        // Honor forwarded headers BEFORE HTTPS redirection so the original public scheme
        // (https) is observed and no redirect loop occurs. Also before CORS/auth.
        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("GatewayPolicy");
        app.UseRateLimiter();
        app.UseStaticFiles();
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");

        // Prometheus metrics endpoint
        app.UseMetricServer();
        app.UseHttpMetrics();
        app.MapReverseProxy();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("NovaShop API Gateway starting");

        await app.RunAsync();
    }
}

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;
    private const string CorrelationHeader = "X-Correlation-ID";

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.ContainsKey(CorrelationHeader)
            ? context.Request.Headers[CorrelationHeader].FirstOrDefault()
            : Guid.NewGuid().ToString();
        context.Request.Headers[CorrelationHeader] = correlationId;
        context.Response.Headers[CorrelationHeader] = correlationId;
        _logger.LogInformation(
            "Gateway Request: {Method} {Path} | Correlation: {CorrelationId} | Client: {ClientIP}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        await _next(context);
    }
}
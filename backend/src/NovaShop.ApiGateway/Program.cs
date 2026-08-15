using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace NovaShop.ApiGateway;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true);

        // Configure reverse proxy from configuration
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
            options.Authority = builder.Configuration["Jwt:Issuer"];
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
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("GatewayPolicy", policy =>
            {
                policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
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
            options.OnRejected = async (context, _) => {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");
            };
        });

        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck("gateway", () => HealthCheckResult.Healthy("Gateway is healthy"));

        // Logging
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);

        builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("GATEWAY_URL") ?? "http://localhost:5100");

        var app = builder.Build();

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

        app.MapReverseProxy();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("NovaShop API Gateway starting on http://localhost:5100");

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
            context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        );

        await _next(context);
    }
}

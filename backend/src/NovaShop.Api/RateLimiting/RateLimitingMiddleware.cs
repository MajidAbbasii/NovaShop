using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NovaShop.Common.Models;

namespace NovaShop.Api.RateLimiting;

/// <summary>Rate-limiting middleware that enforces per-client windows.</summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitSettings _settings;
    private readonly IRateLimitCounterStore _store;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Path prefixes we check to classify endpoints
    private static readonly string[] AuthPrefixes = ["/api/auth/login", "/api/auth/register", "/api/auth/refresh"];

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RateLimitSettings> options,
        IRateLimitCounterStore store,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _settings = options.Value;
        _store = store;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!_settings.Enabled)
        {
            await _next(httpContext);
            return;
        }

        var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = httpContext.Request.Method;

        // Skip health/diag endpoints
        if (path is "/health" or "/hangfire" or "" or "/")
        {
            await _next(httpContext);
            return;
        }

        // Determine client key & policy
        var (clientKey, limit, windowSec) = ResolvePolicy(httpContext, path);

        if (limit <= 0)
        {
            await _next(httpContext);
            return;
        }

        var window = TimeSpan.FromSeconds(windowSec);

        // Get current counter
        var counter = await _store.GetAsync(clientKey);
        var now = DateTime.UtcNow;
        var windowStart = counter?.WindowStartTicks is { } ticks ? new DateTime(ticks, DateTimeKind.Utc) : now;

        // If window expired, reset
        if (counter == null || now - windowStart >= window)
        {
            counter = new RateLimitCounter { Count = 0, WindowStartTicks = now.Ticks };
            windowStart = now;
        }

        var remaining = Math.Max(0, limit - counter.Count);
        var resetTime = windowStart.Add(window);
        var retryAfter = (int)Math.Ceiling((resetTime - now).TotalSeconds);

        // Set response headers
        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            httpContext.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            httpContext.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString();
            return Task.CompletedTask;
        });

        if (counter.Count >= limit)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {ClientKey}. {Count}/{Limit} requests in window.",
                clientKey, counter.Count, limit);

            httpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            httpContext.Response.Headers["Retry-After"] = retryAfter.ToString();

            var problem = new
            {
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Title = "Too Many Requests",
                Status = 429,
                Detail = $"Rate limit exceeded. Try again in {retryAfter} second(s).",
                RetryAfter = retryAfter
            };

            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problem));
            return;
        }

        // Atomically increment via store
        await _store.IncrementAsync(clientKey, window + TimeSpan.FromSeconds(5)); // slight over-TTL for safety

        await _next(httpContext);
    }

    private (string ClientKey, int Limit, int WindowSeconds) ResolvePolicy(HttpContext ctx, string path)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Auth endpoints — lowest limit
        if (AuthPrefixes.Any(p => path.StartsWith(p)))
            return ($"auth:{ip}", _settings.AuthEndpointLimit, _settings.AuthEndpointWindowSeconds);

        // Admin endpoints — highest limit
        if (path.StartsWith("/api/admin"))
            return ($"admin:{ip}", _settings.AdminLimit, _settings.DefaultWindowSeconds);

        // Authenticated user — extract userId from JWT
        var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? ctx.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
            return ($"user:{userId}", _settings.AuthenticatedLimit, _settings.DefaultWindowSeconds);

        // Public / anonymous
        return ($"ip:{ip}", _settings.DefaultLimit, _settings.DefaultWindowSeconds);
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}

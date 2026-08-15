using System.Security.Claims;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NovaShop.Api.Services;

/// <summary>
/// Protects the Hangfire Dashboard. Access is granted only when the caller is an
/// authenticated Admin (cookie/JWT principal with the "Admin" role) OR supplies the
/// configured shared DashboardAccessKey via the "hfkey" query parameter or the
/// "X-Hangfire-Key" header. Without either, the dashboard is not publicly reachable.
/// Keeps operations-safe access without inventing a second auth system.
/// </summary>
public class AdminHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string? _accessKey;
    private readonly ILogger<AdminHangfireAuthorizationFilter> _logger;
    private const string QueryKey = "hfkey";
    private const string HeaderKey = "X-Hangfire-Key";

    public AdminHangfireAuthorizationFilter(string? accessKey, ILogger<AdminHangfireAuthorizationFilter> logger)
    {
        _accessKey = accessKey;
        _logger = logger;
    }

    public bool Authorize(DashboardContext context)
    {
        try
        {
            var http = context.GetHttpContext();
            var path = http.Request.Path.Value ?? string.Empty;

            // Hangfire's embedded static assets (css/js/fonts) are not sensitive and
            // must be served so the dashboard UI works. Allow them without auth.
            if (IsHangfireAsset(path))
                return true;

            // 1) Signed-in admin (role-based).
            var user = http.User;
            if (user?.Identity?.IsAuthenticated == true &&
                user.HasClaim(ClaimTypes.Role, "Admin"))
            {
                _logger.LogDebug("Hangfire auth: granted via Admin role.");
                return true;
            }

            // 2) Shared access key fallback (ops / remote viewing without a session).
            if (!string.IsNullOrWhiteSpace(_accessKey))
            {
                var provided = http.Request.Query[QueryKey].ToString();
                if (string.IsNullOrEmpty(provided))
                    provided = http.Request.Headers[HeaderKey].ToString();

                _logger.LogDebug(
                    "Hangfire auth: key provided='{Provided}' (len {PLen}), configured len {CLen}",
                    provided, provided?.Length ?? 0, _accessKey.Length);

                if (!string.IsNullOrEmpty(provided) &&
                    string.Equals(provided, _accessKey, StringComparison.Ordinal))
                {
                    _logger.LogDebug("Hangfire auth: granted via access key.");
                    return true;
                }
            }
            else
            {
                _logger.LogDebug("Hangfire auth: no access key configured.");
            }

            _logger.LogDebug("Hangfire auth: DENIED.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire auth filter threw.");
            return false;
        }
    }

    private static bool IsHangfireAsset(string path)
    {
        if (!path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase))
            return false;

        // Hangfire serves embedded UI assets at virtual paths like:
        //   /hangfire/css<hash>   /hangfire/css-dark<hash>   /hangfire/js<hash>
        //   /hangfire/fonts/...   and any .css/.js/.woff*/.ttf file.
        if (path.Contains("/fonts", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            return true;

        // Embedded css/js virtual paths have no slash between the segment and hash.
        var withoutPrefix = path["/hangfire".Length..];
        return withoutPrefix.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || withoutPrefix.StartsWith("/js", StringComparison.OrdinalIgnoreCase);
    }
}

namespace NovaShop.Common.Models;

public class RateLimitSettings
{
    public bool Enabled { get; set; } = true;
    public int DefaultLimit { get; set; } = 100;
    public int DefaultWindowSeconds { get; set; } = 60;

    /// <summary>Rate limit for authenticated users (non-admin, non-auth).</summary>
    public int AuthenticatedLimit { get; set; } = 300;

    /// <summary>Rate limit for admin endpoints.</summary>
    public int AdminLimit { get; set; } = 500;

    /// <summary>Rate limit for auth endpoints (login/register/refresh).</summary>
    public int AuthEndpointLimit { get; set; } = 5;
    public int AuthEndpointWindowSeconds { get; set; } = 60;

    public string? RedisConnectionString { get; set; }
    public string InstanceName { get; set; } = "NovaShop_RateLimit_";
}

/// <summary>Per-client counter snapshot.</summary>
public class RateLimitCounter
{
    public int Count { get; set; }
    public long WindowStartTicks { get; set; }
}

using NovaShop.Common.Models;

namespace NovaShop.Api.RateLimiting;

/// <summary>Abstraction for persisting rate-limit counters.</summary>
public interface IRateLimitCounterStore
{
    /// <summary>Get counter for a client key, or null if expired/missing.</summary>
    Task<RateLimitCounter?> GetAsync(string clientKey);

    /// <summary>Set (or overwrite) counter for a client key with TTL.</summary>
    Task SetAsync(string clientKey, RateLimitCounter counter, TimeSpan ttl);

    /// <summary>Increment count atomically; returns updated count.</summary>
    Task<long> IncrementAsync(string clientKey, TimeSpan ttl);
}

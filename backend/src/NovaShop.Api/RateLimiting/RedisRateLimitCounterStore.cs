using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using NovaShop.Common.Models;

namespace NovaShop.Api.RateLimiting;

/// <summary>Redis-backed counter store. Uses the already-installed <see cref="IDistributedCache"/>.</summary>
public class RedisRateLimitCounterStore : IRateLimitCounterStore
{
    private readonly IDistributedCache _cache;
    private readonly string _prefix;

    public RedisRateLimitCounterStore(IDistributedCache cache, string prefix = "NovaShop_RateLimit_")
    {
        _cache = cache;
        _prefix = prefix;
    }

    public async Task<RateLimitCounter?> GetAsync(string clientKey)
    {
        var key = _prefix + clientKey;
        var data = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(data)) return null;
        return JsonSerializer.Deserialize<RateLimitCounter>(data);
    }

    public async Task SetAsync(string clientKey, RateLimitCounter counter, TimeSpan ttl)
    {
        var key = _prefix + clientKey;
        var json = JsonSerializer.Serialize(counter);
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    public async Task<long> IncrementAsync(string clientKey, TimeSpan ttl)
    {
        var key = _prefix + clientKey;
        var data = await _cache.GetStringAsync(key);
        RateLimitCounter counter;

        if (string.IsNullOrEmpty(data))
        {
            counter = new RateLimitCounter { Count = 1, WindowStartTicks = DateTime.UtcNow.Ticks };
        }
        else
        {
            counter = JsonSerializer.Deserialize<RateLimitCounter>(data)!;
            counter.Count++;
        }

        var json = JsonSerializer.Serialize(counter);
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });

        return counter.Count;
    }
}

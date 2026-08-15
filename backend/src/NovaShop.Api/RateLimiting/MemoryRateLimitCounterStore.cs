using System.Collections.Concurrent;
using NovaShop.Common.Models;

namespace NovaShop.Api.RateLimiting;

/// <summary>In-memory (<see cref="ConcurrentDictionary"/>) implementation — fallback when Redis unavailable.</summary>
public class MemoryRateLimitCounterStore : IRateLimitCounterStore, IDisposable
{
    private readonly ConcurrentDictionary<string, (RateLimitCounter Counter, DateTime ExpiresAt)> _store = new();
    private readonly Timer _cleanupTimer;

    public MemoryRateLimitCounterStore()
    {
        // Evict expired entries every 30 seconds
        _cleanupTimer = new Timer(_ =>
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _store)
            {
                if (kv.Value.ExpiresAt <= now)
                    _store.TryRemove(kv.Key, out var _);
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public Task<RateLimitCounter?> GetAsync(string clientKey)
    {
        if (_store.TryGetValue(clientKey, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult<RateLimitCounter?>(entry.Counter);

        return Task.FromResult<RateLimitCounter?>(null);
    }

    public Task SetAsync(string clientKey, RateLimitCounter counter, TimeSpan ttl)
    {
        _store[clientKey] = (counter, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task<long> IncrementAsync(string clientKey, TimeSpan ttl)
    {
        var now = DateTime.UtcNow;
        var entry = _store.AddOrUpdate(clientKey,
            _ => (new RateLimitCounter { Count = 1, WindowStartTicks = now.Ticks }, now.Add(ttl)),
            (_, existing) =>
            {
                // If expired, reset
                if (existing.ExpiresAt <= now)
                    return (new RateLimitCounter { Count = 1, WindowStartTicks = now.Ticks }, now.Add(ttl));

                existing.Counter.Count++;
                return (existing.Counter, existing.ExpiresAt);
            });

        return Task.FromResult((long)entry.Counter.Count);
    }

    public void Dispose() => _cleanupTimer?.Dispose();
}

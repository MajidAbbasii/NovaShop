using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace NovaShop.Application.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
        };

        _cache.Set(key, value, options);
        _keys.TryAdd(key, 0);
    }

    public async Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
    }
}

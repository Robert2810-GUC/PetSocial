using System.Text.Json;
using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);
        return data is null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, json, options);
    }

    public Task RemoveAsync(string key)
    {
        return _cache.RemoveAsync(key);
    }
}


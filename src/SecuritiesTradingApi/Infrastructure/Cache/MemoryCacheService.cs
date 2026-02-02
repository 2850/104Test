using Microsoft.Extensions.Caching.Memory;

namespace SecuritiesTradingApi.Infrastructure.Cache;

public interface IMemoryCacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan expiration);
    void Remove(string key);
}

public class MemoryCacheService : IMemoryCacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        _cache.Set(key, value, expiration);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}

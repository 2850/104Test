using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Infrastructure.Cache;

namespace SecuritiesTradingApi.Infrastructure.ExternalApis;

public class CachedTwseApiClient : ITwseApiClient
{
    private readonly ITwseApiClient _innerClient;
    private readonly IMemoryCacheService _cache;
    private readonly TimeSpan _cacheDuration;
    private const string CacheKeyPrefix = "StockQuote_";

    public CachedTwseApiClient(
        TwseApiClient innerClient,
        IMemoryCacheService cache,
        IConfiguration configuration)
    {
        _innerClient = innerClient;
        _cache = cache;
        _cacheDuration = TimeSpan.FromSeconds(configuration.GetValue<int>("TwseApi:CacheSeconds", 5));
    }

    public async Task<StockQuoteDto?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{stockCode}";
        
        var cachedData = _cache.Get<StockQuoteDto>(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }
        
        var quote = await _innerClient.GetStockQuoteAsync(stockCode, cancellationToken);
        
        if (quote != null)
        {
            _cache.Set(cacheKey, quote, _cacheDuration);
        }
        
        return quote;
    }
}

using Microsoft.EntityFrameworkCore;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Infrastructure.ExternalApis;
using SecuritiesTradingApi.Infrastructure.Cache;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Services;

public class StockService : IStockService
{
    private readonly TradingDbContext _context;
    private readonly ITwseApiClient _twseApiClient;
    private readonly IMemoryCacheService _cacheService;
    private readonly ILogger<StockService> _logger;

    public StockService(
        TradingDbContext context,
        ITwseApiClient twseApiClient,
        IMemoryCacheService cacheService,
        ILogger<StockService> logger)
    {
        _context = context;
        _twseApiClient = twseApiClient;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<StockInfoDto?> GetStockInfoAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying stock info for {StockCode}", stockCode);

        // Try to get from memory cache first
        var cacheKey = $"stock:info:{stockCode}";
        var cachedInfo = _cacheService.Get<StockInfoDto>(cacheKey);
        
        if (cachedInfo != null)
        {
            _logger.LogInformation("Stock info for {StockCode} retrieved from cache", stockCode);
            return cachedInfo;
        }

        _logger.LogInformation("Cache miss for stock info {StockCode}, fetching from database", stockCode);

        var stock = await _context.StockMaster
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StockCode == stockCode, cancellationToken);

        if (stock == null)
        {
            _logger.LogWarning("Stock {StockCode} not found", stockCode);
            return null;
        }

        _logger.LogInformation("Successfully retrieved stock info for {StockCode}", stockCode);

        var stockInfo = new StockInfoDto
        {
            StockCode = stock.StockCode,
            StockName = stock.StockName,
            StockNameShort = stock.StockNameShort,
            StockNameEn = stock.StockNameEn,
            Exchange = stock.Exchange,
            Industry = stock.Industry,
            LotSize = stock.LotSize,
            AllowOddLot = stock.AllowOddLot,
            IsActive = stock.IsActive,
            ListedDate = stock.ListedDate
        };

        // Cache for 5 minutes (stock info changes rarely)
        _cacheService.Set(cacheKey, stockInfo, TimeSpan.FromMinutes(5));
        _logger.LogInformation("Cached stock info for {StockCode} with 5 minutes expiration", stockCode);

        return stockInfo;
    }

    public async Task<PagedResult<StockInfoDto>> SearchStocksAsync(string? symbol = null, string? keyword = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching stocks with symbol={Symbol}, keyword={Keyword}, page={Page}, pageSize={PageSize}", symbol, keyword, page, pageSize);

        // 確保頁碼和頁面大小有效
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // 限制最大頁面大小

        var query = _context.StockMaster.AsNoTracking().AsQueryable();

        // 如果提供了symbol，優先使用symbol查詢
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            query = query.Where(s => s.StockCode == symbol);
        }
        // 否則使用keyword在StockName和StockNameShort中查詢
        else if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(s => s.StockName.Contains(keyword) || s.StockNameShort.Contains(keyword));
        }

        // 獲取總記錄數
        var totalCount = await query.CountAsync(cancellationToken);

        // 分頁查詢
        var stocks = await query
            .OrderBy(s => s.StockCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} stocks (total: {TotalCount})", stocks.Count, totalCount);

        var items = stocks.Select(stock => new StockInfoDto
        {
            StockCode = stock.StockCode,
            StockName = stock.StockName,
            StockNameShort = stock.StockNameShort,
            StockNameEn = stock.StockNameEn,
            Exchange = stock.Exchange,
            Industry = stock.Industry,
            LotSize = stock.LotSize,
            AllowOddLot = stock.AllowOddLot,
            IsActive = stock.IsActive,
            ListedDate = stock.ListedDate
        });

        return PagedResult<StockInfoDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<StockQuoteDto?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying stock quote for {StockCode}", stockCode);

        // Try to get from memory cache first
        var cacheKey = $"stock:quote:{stockCode}";
        var cachedQuote = _cacheService.Get<StockQuoteDto>(cacheKey);
        
        if (cachedQuote != null)
        {
            _logger.LogInformation("Stock quote for {StockCode} retrieved from cache", stockCode);
            return cachedQuote;
        }

        _logger.LogInformation("Cache miss for {StockCode}, fetching from TWSE API", stockCode);

        var startTime = DateTime.UtcNow;

        // Try to get from TWSE API
        var quote = await _twseApiClient.GetStockQuoteAsync(stockCode, cancellationToken);

        var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("TWSE API response time: {ResponseTime}ms for {StockCode}", responseTime, stockCode);

        if (quote != null)
        {
            // Cache the quote for 5 seconds
            _cacheService.Set(cacheKey, quote, TimeSpan.FromSeconds(5));
            _logger.LogInformation("Cached stock quote for {StockCode} with 5 seconds expiration", stockCode);

            // Update snapshot in database
            var snapshot = await _context.StockQuotesSnapshot
                .FirstOrDefaultAsync(s => s.StockCode == stockCode, cancellationToken);

            if (snapshot == null)
            {
                snapshot = new Models.Entities.StockQuotesSnapshot
                {
                    StockCode = stockCode
                };
                _context.StockQuotesSnapshot.Add(snapshot);
            }

            snapshot.CurrentPrice = quote.CurrentPrice;
            snapshot.YesterdayPrice = quote.YesterdayPrice;
            snapshot.OpenPrice = quote.OpenPrice;
            snapshot.HighPrice = quote.HighPrice;
            snapshot.LowPrice = quote.LowPrice;
            snapshot.LimitUpPrice = quote.LimitUpPrice;
            snapshot.LimitDownPrice = quote.LimitDownPrice;
            snapshot.ChangeAmount = quote.ChangeAmount;
            snapshot.ChangePercent = quote.ChangePercent;
            snapshot.TotalVolume = quote.TotalVolume;
            snapshot.TotalValue = quote.TotalValue;
            snapshot.UpdateTime = quote.UpdateTime;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated stock quote snapshot for {StockCode}", stockCode);
        }
        else
        {
            _logger.LogWarning("Failed to fetch quote from TWSE API for {StockCode}", stockCode);
        }

        return quote;
    }
}

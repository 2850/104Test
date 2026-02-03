using Microsoft.EntityFrameworkCore;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Infrastructure.ExternalApis;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Services;

public class StockService : IStockService
{
    private readonly TradingDbContext _context;
    private readonly ITwseApiClient _twseApiClient;
    private readonly ILogger<StockService> _logger;

    public StockService(
        TradingDbContext context,
        ITwseApiClient twseApiClient,
        ILogger<StockService> logger)
    {
        _context = context;
        _twseApiClient = twseApiClient;
        _logger = logger;
    }

    public async Task<StockInfoDto?> GetStockInfoAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying stock info for {StockCode}", stockCode);

        var stock = await _context.StockMaster
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StockCode == stockCode, cancellationToken);

        if (stock == null)
        {
            _logger.LogWarning("Stock {StockCode} not found", stockCode);
            return null;
        }

        _logger.LogInformation("Successfully retrieved stock info for {StockCode}", stockCode);

        return new StockInfoDto
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
    }

    public async Task<IEnumerable<StockInfoDto>> SearchStocksAsync(string? symbol = null, string? keyword = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching stocks with symbol={Symbol}, keyword={Keyword}", symbol, keyword);

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

        var stocks = await query
            .OrderBy(s => s.StockCode)
            .Take(100) // 限制返回數量
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} stocks", stocks.Count);

        return stocks.Select(stock => new StockInfoDto
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
    }

    public async Task<StockQuoteDto?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying stock quote for {StockCode}", stockCode);

        // // First check if stock exists in database
        // var stockExists = await _context.StockMaster
        //     .AsNoTracking()
        //     .AnyAsync(s => s.StockCode == stockCode, cancellationToken);

        // if (!stockExists)
        // {
        //     _logger.LogWarning("Stock {StockCode} not found in database", stockCode);
        //     return null;
        // }

        var startTime = DateTime.UtcNow;

        // Try to get from cache/API
        var quote = await _twseApiClient.GetStockQuoteAsync(stockCode, cancellationToken);

        var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("TWSE API response time: {ResponseTime}ms for {StockCode}", responseTime, stockCode);

        if (quote != null)
        {
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

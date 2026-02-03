using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Services;

public interface IStockService
{
    Task<StockInfoDto?> GetStockInfoAsync(string stockCode, CancellationToken cancellationToken = default);
    Task<PagedResult<StockInfoDto>> SearchStocksAsync(string? symbol = null, string? keyword = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<StockQuoteDto?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default);
}

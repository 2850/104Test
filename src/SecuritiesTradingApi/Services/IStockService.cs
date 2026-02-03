using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Services;

public interface IStockService
{
    Task<StockInfoDto?> GetStockInfoAsync(string stockCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockInfoDto>> SearchStocksAsync(string? symbol = null, string? keyword = null, CancellationToken cancellationToken = default);
    Task<StockQuoteDto?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default);
}

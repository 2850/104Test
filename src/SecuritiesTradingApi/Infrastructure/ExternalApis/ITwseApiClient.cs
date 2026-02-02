using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.Infrastructure.ExternalApis;

public interface ITwseApiClient
{
    Task<StockQuoteDto?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default);
}

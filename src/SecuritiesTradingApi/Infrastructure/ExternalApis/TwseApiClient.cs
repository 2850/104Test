using SecuritiesTradingApi.Models.Dtos;
using System.Text.Json;

namespace SecuritiesTradingApi.Infrastructure.ExternalApis;

public class TwseApiClient : ITwseApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TwseApiClient> _logger;
    private readonly int _maxRetries;
    private static readonly int[] RetryDelaysMs = { 1000, 2000 };

    public TwseApiClient(HttpClient httpClient, ILogger<TwseApiClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _maxRetries = configuration.GetValue<int>("TwseApi:MaxRetries", 2);
    }

    public async Task<StockQuoteDto?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        var url = $"getStockInfo.jsp?ex_ch=tse_{stockCode}.tw";
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogWarning("TWSE API returned 503 for stock {StockCode}, attempt {Attempt}", stockCode, attempt + 1);
                    
                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
                        continue;
                    }
                    return null;
                }
                
                response.EnsureSuccessStatusCode();
                
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<TwseApiResponse>(jsonString, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (apiResponse?.MsgArray == null || apiResponse.MsgArray.Length == 0)
                {
                    return null;
                }
                
                var data = apiResponse.MsgArray[0];
                return MapToStockQuoteDto(stockCode, data);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                _logger.LogWarning(ex, "HTTP request failed for stock {StockCode}, attempt {Attempt}", stockCode, attempt + 1);
                await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stock quote for {StockCode}", stockCode);
                throw;
            }
        }
        
        return null;
    }

    private static StockQuoteDto MapToStockQuoteDto(string stockCode, TwseMsgArray data)
    {
        // 輔助方法：安全解析 decimal，處理 "-" 或空值
        static decimal SafeParseDecimal(string? value, decimal defaultValue = 0m)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return defaultValue;
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }

        // 輔助方法：安全解析 long，處理 "-" 或空值
        static long SafeParseLong(string? value, long defaultValue = 0L)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return defaultValue;
            return long.TryParse(value, out var result) ? result : defaultValue;
        }

        var currentPrice = SafeParseDecimal(data.Z);
        var yesterdayPrice = SafeParseDecimal(data.Y);
        
        return new StockQuoteDto
        {
            StockCode = stockCode,
            CurrentPrice = currentPrice,
            YesterdayPrice = yesterdayPrice,
            OpenPrice = SafeParseDecimal(data.O),
            HighPrice = SafeParseDecimal(data.H),
            LowPrice = SafeParseDecimal(data.L),
            LimitUpPrice = SafeParseDecimal(data.U),
            LimitDownPrice = SafeParseDecimal(data.W),
            ChangeAmount = currentPrice - yesterdayPrice,
            ChangePercent = yesterdayPrice > 0 ? (currentPrice - yesterdayPrice) / yesterdayPrice * 100 : 0,
            TotalVolume = SafeParseLong(data.V),
            TotalValue = string.IsNullOrEmpty(data.Tv) || data.Tv == "-" ? null : SafeParseDecimal(data.Tv),
            UpdateTime = DateTime.UtcNow
        };
    }

    private class TwseApiResponse
    {
        public TwseMsgArray[]? MsgArray { get; set; }
    }

    private class TwseMsgArray
    {
        public string? Z { get; set; }  // CurrentPrice
        public string? Y { get; set; }  // YesterdayPrice
        public string? O { get; set; }  // OpenPrice
        public string? H { get; set; }  // HighPrice
        public string? L { get; set; }  // LowPrice
        public string? U { get; set; }  // LimitUpPrice
        public string? W { get; set; }  // LimitDownPrice
        public string? V { get; set; }  // TotalVolume
        public string? Tv { get; set; } // TotalValue
    }
}

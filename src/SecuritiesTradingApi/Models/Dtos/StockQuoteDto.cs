namespace SecuritiesTradingApi.Models.Dtos;

public class StockQuoteDto
{
    public string StockCode { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public decimal YesterdayPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal LimitUpPrice { get; set; }
    public decimal LimitDownPrice { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public long TotalVolume { get; set; }
    public decimal? TotalValue { get; set; }
    public DateTime UpdateTime { get; set; }
}

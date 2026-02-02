namespace SecuritiesTradingApi.Models.Dtos;

public class StockInfoDto
{
    public string StockCode { get; set; } = null!;
    public string StockName { get; set; } = null!;
    public string StockNameShort { get; set; } = null!;
    public string? StockNameEn { get; set; }
    public string Exchange { get; set; } = null!;
    public string? Industry { get; set; }
    public int LotSize { get; set; }
    public bool AllowOddLot { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ListedDate { get; set; }
}

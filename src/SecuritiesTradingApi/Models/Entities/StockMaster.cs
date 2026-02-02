namespace SecuritiesTradingApi.Models.Entities;

public class StockMaster
{
    public string StockCode { get; set; } = null!;
    public string StockName { get; set; } = null!;
    public string StockNameShort { get; set; } = null!;
    public string? StockNameEn { get; set; }
    public string Exchange { get; set; } = null!;
    public string? Industry { get; set; }
    public int LotSize { get; set; } = 1000;
    public bool AllowOddLot { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? ListedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<OrdersWrite> OrdersWrite { get; set; } = new List<OrdersWrite>();
    public ICollection<OrdersRead> OrdersRead { get; set; } = new List<OrdersRead>();
}

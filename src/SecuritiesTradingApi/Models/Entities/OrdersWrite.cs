namespace SecuritiesTradingApi.Models.Entities;

public class OrdersWrite
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public string StockCode { get; set; } = null!;
    public byte OrderType { get; set; }
    public byte BuySell { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public byte OrderStatus { get; set; } = 1;
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public long OrderSeq { get; set; }

    // Navigation Properties
    public StockMaster Stock { get; set; } = null!;
}

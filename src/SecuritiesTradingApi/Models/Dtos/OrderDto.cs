namespace SecuritiesTradingApi.Models.Dtos;

public class OrderDto
{
    public long OrderId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string StockCode { get; set; } = null!;
    public string? StockName { get; set; }
    public string? StockNameShort { get; set; }
    public byte OrderType { get; set; }
    public string? OrderTypeName { get; set; }
    public byte BuySell { get; set; }
    public string? BuySellName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int FilledQuantity { get; set; }
    public byte OrderStatus { get; set; }
    public string? OrderStatusName { get; set; }
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

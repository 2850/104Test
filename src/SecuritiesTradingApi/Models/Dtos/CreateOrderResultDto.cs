namespace SecuritiesTradingApi.Models.Dtos;

public class CreateOrderResultDto
{
    public long OrderId { get; set; }
    public string StockCode { get; set; } = null!;
    public string StockName { get; set; } = null!;
    public byte OrderType { get; set; }
    public string OrderTypeName { get; set; } = null!;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public byte OrderStatus { get; set; }
    public string OrderStatusName { get; set; } = null!;
    public DateTime TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

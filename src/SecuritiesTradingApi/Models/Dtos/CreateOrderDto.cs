namespace SecuritiesTradingApi.Models.Dtos;

public class CreateOrderDto
{
    public int UserId { get; set; }
    public string StockCode { get; set; } = null!;
    public byte OrderType { get; set; }
    public byte BuySell { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

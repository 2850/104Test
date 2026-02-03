using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Services;

public class OrderService : IOrderService
{
    private readonly TradingDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(TradingDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto orderDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for user {UserId}, stock {StockCode}, quantity {Quantity}", 
            orderDto.UserId, orderDto.StockCode, orderDto.Quantity);

        // Validate stock exists
        var stock = await _context.StockMaster
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StockCode == orderDto.StockCode, cancellationToken);

        if (stock == null)
        {
            _logger.LogWarning("Order creation failed: Stock {StockCode} not found", orderDto.StockCode);
            throw new KeyNotFoundException($"Stock {orderDto.StockCode} not found");
        }

        // Validate price within limit up/down range
        var quote = await _context.StockQuotesSnapshot
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StockCode == orderDto.StockCode, cancellationToken);

        if (quote != null)
        {
            if (orderDto.Price > quote.LimitUpPrice || orderDto.Price < quote.LimitDownPrice)
            {_logger.LogWarning("Order creation failed: Price {Price} outside limit range ({LimitDown}-{LimitUp}) for {StockCode}",
                    orderDto.Price, quote.LimitDownPrice, quote.LimitUpPrice, orderDto.StockCode);
                
                throw new ArgumentException($"Price {orderDto.Price} is outside limit range ({quote.LimitDownPrice} - {quote.LimitUpPrice})");
            }
        }

        // Generate OrderId using sequence
        var orderIdParam = new SqlParameter("@Result", System.Data.SqlDbType.BigInt)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        await _context.Database.ExecuteSqlRawAsync(
            "SET @Result = NEXT VALUE FOR seq_OrderSequence",
            new[] { orderIdParam },
            cancellationToken);

        var orderId = (long)orderIdParam.Value!;
        var tradeDate = DateTime.UtcNow.Date;
        var createdAt = DateTime.UtcNow;

        // Write to Orders_Write (hot layer)
        var orderWrite = new OrdersWrite
        {
            OrderId = orderId,
            UserId = orderDto.UserId,
            StockCode = orderDto.StockCode,
            OrderType = orderDto.OrderType,
            BuySell = orderDto.BuySell,
            Price = orderDto.Price,
            Quantity = orderDto.Quantity,
            OrderStatus = 1, // Pending
            TradeDate = tradeDate,
            CreatedAt = createdAt,
            OrderSeq = orderId
        };

        _context.OrdersWrite.Add(orderWrite);
        await _context.SaveChangesAsync(cancellationToken);

        // Sync to Orders_Read (warm layer)
        var orderRead = new OrdersRead
        {
            OrderId = orderId,
            UserId = orderDto.UserId,
            StockCode = orderDto.StockCode,
            StockName = stock.StockName,
            StockNameShort = stock.StockNameShort,
            OrderType = orderDto.OrderType,
            BuySell = orderDto.BuySell,
            OrderTypeName = orderDto.BuySell == 1 ? "Buy" : "Sell",
            Price = orderDto.Price,
            Quantity = orderDto.Quantity,
            FilledQuantity = 0,
            OrderStatus = 1,
            OrderStatusName = "Pending",
            TradeDate = tradeDate,
            CreatedAt = createdAt,
            OrderSeq = orderId
        };

        _context.OrdersRead.Add(orderRead);
        _logger.LogInformation("Successfully created order {OrderId} for user {UserId}, stock {StockCode}",
            orderId, orderDto.UserId, orderDto.StockCode);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateOrderResultDto
        {
            OrderId = orderId,
            StockCode = orderDto.StockCode,
            StockName = stock.StockName,
            OrderType = orderDto.OrderType,
            OrderTypeName = orderDto.OrderType == 1 ? "Buy" : "Sell",
            Price = orderDto.Price,
            Quantity = orderDto.Quantity,
            OrderStatus = 1,
            OrderStatusName = "Pending",
            TradeDate = tradeDate,
            CreatedAt = createdAt
        };
    }

    public async Task<OrderDto?> GetOrderByIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.OrdersRead
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (order == null)
        {
            return null;
        }

        return new OrderDto
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            UserName = order.UserName,
            StockCode = order.StockCode,
            StockName = order.StockName,
            StockNameShort = order.StockNameShort,
            OrderType = order.OrderType,
            OrderTypeName = order.OrderTypeName,
            Price = order.Price,
            Quantity = order.Quantity,
            FilledQuantity = order.FilledQuantity,
            OrderStatus = order.OrderStatus,
            OrderStatusName = order.OrderStatusName,
            TradeDate = order.TradeDate,
            CreatedAt = order.CreatedAt
        };
    }
}

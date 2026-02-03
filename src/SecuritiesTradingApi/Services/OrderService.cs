using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Infrastructure.Cache;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.Services;

public class OrderService : IOrderService
{
    private readonly TradingDbContext _context;
    private readonly IMemoryCacheService _cacheService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        TradingDbContext context,
        IMemoryCacheService cacheService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto orderDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for user {UserId}, stock {StockCode}, quantity {Quantity}", 
            orderDto.UserId, orderDto.StockCode, orderDto.Quantity);

        // Try to get stock from cache first
        var cacheKey = $"stock:master:{orderDto.StockCode}";
        var stock = _cacheService.Get<StockMaster>(cacheKey);

        if (stock == null)
        {
            // Validate stock exists in database
            stock = await _context.StockMaster
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StockCode == orderDto.StockCode, cancellationToken);

            if (stock == null)
            {
                _logger.LogWarning("Order creation failed: Stock {StockCode} not found", orderDto.StockCode);
                throw new KeyNotFoundException($"Stock {orderDto.StockCode} not found");
            }

            // Cache for 5 minutes
            _cacheService.Set(cacheKey, stock, TimeSpan.FromMinutes(5));
            _logger.LogInformation("Cached stock master for {StockCode}", orderDto.StockCode);
        }
        else
        {
            _logger.LogInformation("Stock {StockCode} retrieved from cache", orderDto.StockCode);
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
        
        // Sync to Orders_Read (warm layer)
        var orderRead = new OrdersRead
        {
            OrderId = orderId,
            UserId = orderDto.UserId,
            StockCode = orderDto.StockCode,
            StockName = stock.StockName,
            StockNameShort = stock.StockNameShort,
            OrderType = orderDto.OrderType,
            OrderTypeName = orderDto.OrderType == 1 ? "Limit" : "Market",
            BuySell = orderDto.BuySell,
            BuySellName = orderDto.BuySell == 1 ? "Buy" : "Sell",
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

        // ✅ 合并为一次保存（改进性能）
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateOrderResultDto
        {
            OrderId = orderId,
            StockCode = orderDto.StockCode,
            StockName = stock.StockName,
            OrderType = orderDto.OrderType,
            OrderTypeName = orderDto.OrderType == 1 ? "Limit" : "Market",
            BuySell = orderDto.BuySell,
            BuySellName = orderDto.BuySell == 1 ? "Buy" : "Sell",
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
            BuySell = order.BuySell,
            BuySellName = order.BuySellName,
            Price = order.Price,
            Quantity = order.Quantity,
            FilledQuantity = order.FilledQuantity,
            OrderStatus = order.OrderStatus,
            OrderStatusName = order.OrderStatusName,
            TradeDate = order.TradeDate,
            CreatedAt = order.CreatedAt
        };
    }

    public async Task<List<OrderDto>> GetOrdersAsync(long? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting orders{UserFilter}", userId.HasValue ? $" for user {userId}" : "");

        var query = _context.OrdersRead.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(order => new OrderDto
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            UserName = order.UserName,
            StockCode = order.StockCode,
            StockName = order.StockName,
            StockNameShort = order.StockNameShort,
            OrderType = order.OrderType,
            OrderTypeName = order.OrderTypeName,
            BuySell = order.BuySell,
            BuySellName = order.BuySellName,
            Price = order.Price,
            Quantity = order.Quantity,
            FilledQuantity = order.FilledQuantity,
            OrderStatus = order.OrderStatus,
            OrderStatusName = order.OrderStatusName,
            TradeDate = order.TradeDate,
            CreatedAt = order.CreatedAt
        }).ToList();
    }
}

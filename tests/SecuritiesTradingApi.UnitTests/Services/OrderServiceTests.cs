using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Models.Entities;
using SecuritiesTradingApi.Services;

namespace SecuritiesTradingApi.UnitTests.Services;

public class OrderServiceTests
{
    private readonly Mock<ILogger<OrderService>> _loggerMock;

    public OrderServiceTests()
    {
        _loggerMock = new Mock<ILogger<OrderService>>();
    }

    private TradingDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TradingDbContext(options);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var order = new OrdersRead
        {
            OrderId = 1,
            UserId = 1,
            StockCode = "2330",
            StockName = "台積電",
            StockNameShort = "台積電",
            OrderType = 1,
            OrderTypeName = "Buy",
            Price = 580.00m,
            Quantity = 1000,
            FilledQuantity = 0,
            OrderStatus = 1,
            OrderStatusName = "Pending",
            TradeDate = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow,
            OrderSeq = 1
        };
        context.OrdersRead.Add(order);
        await context.SaveChangesAsync();

        var service = new OrderService(context, _loggerMock.Object);

        // Act
        var result = await service.GetOrderByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(1);
        result.StockCode.Should().Be("2330");
        result.StockName.Should().Be("台積電");
        result.OrderType.Should().Be(1);
        result.Price.Should().Be(580.00m);
        result.Quantity.Should().Be(1000);
    }

    [Fact]
    public async Task GetOrderByIdAsync_NonExistingOrder_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new OrderService(context, _loggerMock.Object);

        // Act
        var result = await service.GetOrderByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }
}

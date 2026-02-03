using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SecuritiesTradingApi.Data;
using SecuritiesTradingApi.Infrastructure.ExternalApis;
using SecuritiesTradingApi.Infrastructure.Cache;
using SecuritiesTradingApi.Models.Entities;
using SecuritiesTradingApi.Services;

namespace SecuritiesTradingApi.UnitTests.Services;

public class StockServiceTests
{
    private readonly Mock<ILogger<StockService>> _loggerMock;
    private readonly Mock<IMemoryCacheService> _cacheMock;

    public StockServiceTests()
    {
        _loggerMock = new Mock<ILogger<StockService>>();
        _cacheMock = new Mock<IMemoryCacheService>();
    }

    private TradingDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TradingDbContext(options);
    }

    [Fact]
    public async Task GetStockInfoAsync_ExistingStock_ReturnsStockInfo()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var stock = new StockMaster
        {
            StockCode = "2330",
            StockName = "台積電",
            StockNameShort = "台積電",
            StockNameEn = "TSMC",
            Exchange = "TWSE",
            Industry = "半導體業",
            LotSize = 1000,
            AllowOddLot = true,
            IsActive = true
        };
        context.StockMaster.Add(stock);
        await context.SaveChangesAsync();

        var twseClientMock = new Mock<ITwseApiClient>();
        var service = new StockService(context, twseClientMock.Object, _cacheMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetStockInfoAsync("2330");

        // Assert
        result.Should().NotBeNull();
        result!.StockCode.Should().Be("2330");
        result.StockName.Should().Be("台積電");
        result.Exchange.Should().Be("TWSE");
        result.LotSize.Should().Be(1000);
    }

    [Fact]
    public async Task GetStockInfoAsync_NonExistingStock_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var twseClientMock = new Mock<ITwseApiClient>();
        var service = new StockService(context, twseClientMock.Object, _cacheMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetStockInfoAsync("9999");

        // Assert
        result.Should().BeNull();
    }
}

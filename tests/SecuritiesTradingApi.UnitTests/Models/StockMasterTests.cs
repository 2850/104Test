using FluentAssertions;
using SecuritiesTradingApi.Models.Entities;

namespace SecuritiesTradingApi.UnitTests.Models;

public class StockMasterTests
{
    [Fact]
    public void StockMaster_ShouldHaveValidProperties()
    {
        // Arrange & Act
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
            IsActive = true,
            ListedDate = new DateTime(1994, 9, 5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        stock.StockCode.Should().Be("2330");
        stock.StockName.Should().Be("台積電");
        stock.LotSize.Should().Be(1000);
        stock.IsActive.Should().BeTrue();
    }

    [Fact]
    public void StockMaster_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var stock = new StockMaster();

        // Assert
        stock.LotSize.Should().Be(1000);
        stock.AllowOddLot.Should().BeFalse();
        stock.IsActive.Should().BeTrue();
    }
}

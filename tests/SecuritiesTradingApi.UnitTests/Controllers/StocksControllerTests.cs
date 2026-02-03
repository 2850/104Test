using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SecuritiesTradingApi.Controllers;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Services;

namespace SecuritiesTradingApi.UnitTests.Controllers;

public class StocksControllerTests
{
    private readonly Mock<IStockService> _mockStockService;
    private readonly Mock<ILogger<StocksController>> _mockLogger;
    private readonly StocksController _controller;

    public StocksControllerTests()
    {
        _mockStockService = new Mock<IStockService>();
        _mockLogger = new Mock<ILogger<StocksController>>();
        _controller = new StocksController(_mockStockService.Object, _mockLogger.Object);
    }

    #region SearchStocks Tests

    [Fact]
    public async Task SearchStocks_WithSymbol_ReturnsOkWithPagedResult()
    {
        // Arrange
        var symbol = "2330";
        var expectedResult = new PagedResult<StockInfoDto>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1,
            Items = new List<StockInfoDto>
            {
                new StockInfoDto
                {
                    StockCode = "2330",
                    StockName = "台積電",
                    StockNameShort = "台積電",
                    Exchange = "TWSE",
                    LotSize = 1000,
                    AllowOddLot = true,
                    IsActive = true
                }
            }
        };

        _mockStockService
            .Setup(x => x.SearchStocksAsync(symbol, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchStocks(symbol, null, 1, 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<StockInfoDto>>().Subject;
        pagedResult.Items.Should().HaveCount(1);
        pagedResult.Items.First().StockCode.Should().Be("2330");
    }

    [Fact]
    public async Task SearchStocks_WithKeyword_ReturnsOkWithPagedResult()
    {
        // Arrange
        var keyword = "台積";
        var expectedResult = new PagedResult<StockInfoDto>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1,
            Items = new List<StockInfoDto>
            {
                new StockInfoDto
                {
                    StockCode = "2330",
                    StockName = "台積電",
                    StockNameShort = "台積電",
                    Exchange = "TWSE",
                    LotSize = 1000,
                    AllowOddLot = true,
                    IsActive = true
                }
            }
        };

        _mockStockService
            .Setup(x => x.SearchStocksAsync(null, keyword, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchStocks(null, keyword, 1, 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<StockInfoDto>>().Subject;
        pagedResult.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchStocks_WithCustomPagination_ReturnsCorrectPage()
    {
        // Arrange
        var page = 2;
        var pageSize = 10;
        var expectedResult = new PagedResult<StockInfoDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = 25,
            TotalPages = 3,
            Items = new List<StockInfoDto>()
        };

        _mockStockService
            .Setup(x => x.SearchStocksAsync(null, null, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchStocks(null, null, page, pageSize);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<StockInfoDto>>().Subject;
        pagedResult.Page.Should().Be(page);
        pagedResult.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task SearchStocks_NoParameters_ReturnsAllStocks()
    {
        // Arrange
        var expectedResult = new PagedResult<StockInfoDto>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 100,
            TotalPages = 5,
            Items = new List<StockInfoDto>()
        };

        _mockStockService
            .Setup(x => x.SearchStocksAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchStocks(null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<PagedResult<StockInfoDto>>();
    }

    #endregion

    #region GetStockInfo Tests

    [Fact]
    public async Task GetStockInfo_WithValidSymbol_ReturnsOkWithStockInfo()
    {
        // Arrange
        var symbol = "2330";
        var expectedStock = new StockInfoDto
        {
            StockCode = "2330",
            StockName = "台積電",
            StockNameShort = "台積電",
            Exchange = "TWSE",
            Industry = "半導體業",
            LotSize = 1000,
            AllowOddLot = true,
            IsActive = true,
            ListedDate = new DateTime(1994, 9, 5)
        };

        _mockStockService
            .Setup(x => x.GetStockInfoAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStock);

        // Act
        var result = await _controller.GetStockInfo(symbol, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var stockInfo = okResult.Value.Should().BeOfType<StockInfoDto>().Subject;
        stockInfo.StockCode.Should().Be("2330");
        stockInfo.StockName.Should().Be("台積電");
    }

    [Fact]
    public async Task GetStockInfo_WithInvalidSymbol_ReturnsNotFound()
    {
        // Arrange
        var symbol = "9999";
        _mockStockService
            .Setup(x => x.GetStockInfoAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockInfoDto?)null);

        // Act
        var result = await _controller.GetStockInfo(symbol, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Contain("9999");
    }

    [Fact]
    public async Task GetStockInfo_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var symbol = "2330";
        _mockStockService
            .Setup(x => x.GetStockInfoAsync(symbol, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _controller.GetStockInfo(symbol, CancellationToken.None));
    }

    #endregion

    #region GetStockQuote Tests

    [Fact]
    public async Task GetStockQuote_WithValidSymbol_ReturnsOkWithQuote()
    {
        // Arrange
        var symbol = "2330";
        var expectedQuote = new StockQuoteDto
        {
            StockCode = "2330",
            CurrentPrice = 600.00m,
            YesterdayPrice = 595.00m,
            OpenPrice = 598.00m,
            HighPrice = 605.00m,
            LowPrice = 597.00m,
            LimitUpPrice = 654.50m,
            LimitDownPrice = 535.50m,
            ChangeAmount = 5.00m,
            ChangePercent = 0.84m,
            TotalVolume = 25000000,
            TotalValue = 15000000000m,
            UpdateTime = DateTime.Now
        };

        _mockStockService
            .Setup(x => x.GetStockQuoteAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedQuote);

        // Act
        var result = await _controller.GetStockQuote(symbol, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var quote = okResult.Value.Should().BeOfType<StockQuoteDto>().Subject;
        quote.StockCode.Should().Be("2330");
        quote.CurrentPrice.Should().Be(600.00m);
        quote.ChangeAmount.Should().Be(5.00m);
    }

    [Fact]
    public async Task GetStockQuote_ServiceUnavailable_ReturnsServiceUnavailable()
    {
        // Arrange
        var symbol = "2330";
        _mockStockService
            .Setup(x => x.GetStockQuoteAsync(symbol, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockQuoteDto?)null);

        // Act
        var result = await _controller.GetStockQuote(symbol, CancellationToken.None);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        var problemDetails = statusCodeResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(503);
        problemDetails.Title.Should().Be("Service Unavailable");
        problemDetails.Detail.Should().Contain("Unable to fetch stock quote");
    }

    [Fact]
    public async Task GetStockQuote_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var symbol = "2330";
        var cancellationToken = new CancellationToken();
        var expectedQuote = new StockQuoteDto
        {
            StockCode = "2330",
            CurrentPrice = 600.00m,
            YesterdayPrice = 595.00m,
            OpenPrice = 598.00m,
            HighPrice = 605.00m,
            LowPrice = 597.00m,
            LimitUpPrice = 654.50m,
            LimitDownPrice = 535.50m,
            ChangeAmount = 5.00m,
            ChangePercent = 0.84m,
            TotalVolume = 25000000,
            UpdateTime = DateTime.Now
        };

        _mockStockService
            .Setup(x => x.GetStockQuoteAsync(symbol, cancellationToken))
            .ReturnsAsync(expectedQuote);

        // Act
        var result = await _controller.GetStockQuote(symbol, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockStockService.Verify(
            x => x.GetStockQuoteAsync(symbol, cancellationToken),
            Times.Once);
    }

    #endregion
}

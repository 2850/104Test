using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SecuritiesTradingApi.Controllers;
using SecuritiesTradingApi.Models.Dtos;
using SecuritiesTradingApi.Services;

namespace SecuritiesTradingApi.UnitTests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<IValidator<CreateOrderDto>> _mockValidator;
    private readonly Mock<ILogger<OrdersController>> _mockLogger;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockValidator = new Mock<IValidator<CreateOrderDto>>();
        _mockLogger = new Mock<ILogger<OrdersController>>();
        _controller = new OrdersController(
            _mockOrderService.Object,
            _mockValidator.Object,
            _mockLogger.Object);
    }

    #region GetOrders Tests

    [Fact]
    public async Task GetOrders_WithoutUserId_ReturnsAllOrders()
    {
        // Arrange
        var expectedOrders = new List<OrderDto>
        {
            new OrderDto
            {
                OrderId = 1,
                UserId = 1,
                StockCode = "2330",
                StockName = "台積電",
                OrderType = 0,
                OrderTypeName = "現股",
                BuySell = 0,
                BuySellName = "買進",
                Price = 600.00m,
                Quantity = 1000,
                FilledQuantity = 0,
                OrderStatus = 1,
                OrderStatusName = "委託中",
                TradeDate = DateTime.Today,
                CreatedAt = DateTime.Now
            },
            new OrderDto
            {
                OrderId = 2,
                UserId = 2,
                StockCode = "2317",
                StockName = "鴻海",
                OrderType = 0,
                OrderTypeName = "現股",
                BuySell = 1,
                BuySellName = "賣出",
                Price = 105.00m,
                Quantity = 2000,
                FilledQuantity = 2000,
                OrderStatus = 2,
                OrderStatusName = "完全成交",
                TradeDate = DateTime.Today,
                CreatedAt = DateTime.Now
            }
        };

        _mockOrderService
            .Setup(x => x.GetOrdersAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetOrders(null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var orders = okResult.Value.Should().BeAssignableTo<List<OrderDto>>().Subject;
        orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrders_WithUserId_ReturnsUserOrders()
    {
        // Arrange
        var userId = 1L;
        var expectedOrders = new List<OrderDto>
        {
            new OrderDto
            {
                OrderId = 1,
                UserId = 1,
                StockCode = "2330",
                StockName = "台積電",
                OrderType = 0,
                OrderTypeName = "現股",
                BuySell = 0,
                BuySellName = "買進",
                Price = 600.00m,
                Quantity = 1000,
                FilledQuantity = 0,
                OrderStatus = 1,
                OrderStatusName = "委託中",
                TradeDate = DateTime.Today,
                CreatedAt = DateTime.Now
            }
        };

        _mockOrderService
            .Setup(x => x.GetOrdersAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetOrders(userId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<List<OrderDto>>().Subject;
        orders.Should().HaveCount(1);
        orders.First().UserId.Should().Be(1);
    }

    [Fact]
    public async Task GetOrders_NoOrdersFound_ReturnsEmptyList()
    {
        // Arrange
        _mockOrderService
            .Setup(x => x.GetOrdersAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderDto>());

        // Act
        var result = await _controller.GetOrders(null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var orders = okResult.Value.Should().BeAssignableTo<List<OrderDto>>().Subject;
        orders.Should().BeEmpty();
    }

    #endregion

    #region CreateOrder Tests

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 0,
            BuySell = 0,
            Price = 600.00m,
            Quantity = 1000
        };

        var expectedResult = new CreateOrderResultDto
        {
            OrderId = 1,
            StockCode = "2330",
            StockName = "台積電",
            OrderType = 0,
            OrderTypeName = "現股",
            BuySell = 0,
            BuySellName = "買進",
            Price = 600.00m,
            Quantity = 1000,
            OrderStatus = 1,
            OrderStatusName = "委託中",
            TradeDate = DateTime.Today,
            CreatedAt = DateTime.Now
        };

        _mockValidator
            .Setup(x => x.ValidateAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(x => x.CreateOrderAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateOrder(createOrderDto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(OrdersController.CreateOrder));
        var orderResult = createdResult.Value.Should().BeOfType<CreateOrderResultDto>().Subject;
        orderResult.OrderId.Should().Be(1);
        orderResult.StockCode.Should().Be("2330");
    }

    [Fact]
    public async Task CreateOrder_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 0,
            BuySell = 0,
            Price = -100m, // Invalid price
            Quantity = 1000
        };

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("Price", "價格必須大於0")
        };
        var validationResult = new ValidationResult(validationErrors);

        _mockValidator
            .Setup(x => x.ValidateAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.CreateOrder(createOrderDto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Validation Failed");
        problemDetails.Detail.Should().Contain("價格必須大於0");
    }

    [Fact]
    public async Task CreateOrder_WithNonExistentStock_ReturnsNotFound()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "9999",
            OrderType = 0,
            BuySell = 0,
            Price = 100m,
            Quantity = 1000
        };

        _mockValidator
            .Setup(x => x.ValidateAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(x => x.CreateOrderAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Stock 9999 not found"));

        // Act
        var result = await _controller.CreateOrder(createOrderDto, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(404);
        problemDetails.Detail.Should().Contain("9999");
    }

    [Fact]
    public async Task CreateOrder_WithInvalidArgument_ReturnsBadRequest()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 0,
            BuySell = 0,
            Price = 100m,
            Quantity = 500 // Not a multiple of lot size
        };

        _mockValidator
            .Setup(x => x.ValidateAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(x => x.CreateOrderAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Quantity must be a multiple of lot size"));

        // Act
        var result = await _controller.CreateOrder(createOrderDto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(400);
        problemDetails.Detail.Should().Contain("multiple of lot size");
    }

    [Fact]
    public async Task CreateOrder_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            UserId = 0,
            StockCode = "",
            OrderType = 0,
            BuySell = 0,
            Price = -100m,
            Quantity = -1000
        };

        var validationErrors = new List<ValidationFailure>
        {
            new ValidationFailure("UserId", "使用者ID必須大於0"),
            new ValidationFailure("StockCode", "股票代碼不可為空"),
            new ValidationFailure("Price", "價格必須大於0"),
            new ValidationFailure("Quantity", "數量必須大於0")
        };
        var validationResult = new ValidationResult(validationErrors);

        _mockValidator
            .Setup(x => x.ValidateAsync(createOrderDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.CreateOrder(createOrderDto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("使用者ID必須大於0");
        problemDetails.Detail.Should().Contain("股票代碼不可為空");
        problemDetails.Detail.Should().Contain("價格必須大於0");
        problemDetails.Detail.Should().Contain("數量必須大於0");
    }

    #endregion

    #region GetOrder Tests

    [Fact]
    public async Task GetOrder_WithValidOrderId_ReturnsOkWithOrder()
    {
        // Arrange
        var orderId = 1L;
        var expectedOrder = new OrderDto
        {
            OrderId = 1,
            UserId = 1,
            UserName = "測試使用者",
            StockCode = "2330",
            StockName = "台積電",
            StockNameShort = "台積電",
            OrderType = 0,
            OrderTypeName = "現股",
            BuySell = 0,
            BuySellName = "買進",
            Price = 600.00m,
            Quantity = 1000,
            FilledQuantity = 500,
            OrderStatus = 3,
            OrderStatusName = "部分成交",
            TradeDate = DateTime.Today,
            CreatedAt = DateTime.Now
        };

        _mockOrderService
            .Setup(x => x.GetOrderByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _controller.GetOrder(orderId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var order = okResult.Value.Should().BeOfType<OrderDto>().Subject;
        order.OrderId.Should().Be(1);
        order.StockCode.Should().Be("2330");
        order.FilledQuantity.Should().Be(500);
    }

    [Fact]
    public async Task GetOrder_WithInvalidOrderId_ReturnsNotFound()
    {
        // Arrange
        var orderId = 999L;
        _mockOrderService
            .Setup(x => x.GetOrderByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDto?)null);

        // Act
        var result = await _controller.GetOrder(orderId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Contain("999");
    }

    [Fact]
    public async Task GetOrder_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        var orderId = 1L;
        _mockOrderService
            .Setup(x => x.GetOrderByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _controller.GetOrder(orderId, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrder_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var orderId = 1L;
        var cancellationToken = new CancellationToken();
        var expectedOrder = new OrderDto
        {
            OrderId = 1,
            UserId = 1,
            StockCode = "2330",
            StockName = "台積電",
            OrderType = 0,
            BuySell = 0,
            Price = 600.00m,
            Quantity = 1000,
            FilledQuantity = 0,
            OrderStatus = 1,
            TradeDate = DateTime.Today,
            CreatedAt = DateTime.Now
        };

        _mockOrderService
            .Setup(x => x.GetOrderByIdAsync(orderId, cancellationToken))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _controller.GetOrder(orderId, cancellationToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockOrderService.Verify(
            x => x.GetOrderByIdAsync(orderId, cancellationToken),
            Times.Once);
    }

    #endregion
}

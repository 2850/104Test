using FluentAssertions;
using FluentValidation.TestHelper;
using SecuritiesTradingApi.Infrastructure.Validators;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.UnitTests.Validators;

public class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator;

    public CreateOrderValidatorTests()
    {
        _validator = new CreateOrderValidator();
    }

    [Fact]
    public void Validate_ValidOrder_ShouldNotHaveErrors()
    {
        // Arrange
        var order = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 1,
            Price = 580.00m,
            Quantity = 1000
        };

        // Act
        var result = _validator.TestValidate(order);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidUserId_ShouldHaveError(int userId)
    {
        // Arrange
        var order = new CreateOrderDto
        {
            UserId = userId,
            StockCode = "2330",
            OrderType = 1,
            Price = 580.00m,
            Quantity = 1000
        };

        // Act
        var result = _validator.TestValidate(order);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12345678901")] // Too long
    public void Validate_InvalidStockCode_ShouldHaveError(string stockCode)
    {
        // Arrange
        var order = new CreateOrderDto
        {
            UserId = 1,
            StockCode = stockCode!,
            OrderType = 1,
            Price = 580.00m,
            Quantity = 1000
        };

        // Act
        var result = _validator.TestValidate(order);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StockCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(255)]
    public void Validate_InvalidOrderType_ShouldHaveError(byte orderType)
    {
        // Arrange
        var order = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = orderType,
            Price = 580.00m,
            Quantity = 1000
        };

        // Act
        var result = _validator.TestValidate(order);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(10000000)] // Too high
    public void Validate_InvalidPrice_ShouldHaveError(decimal price)
    {
        // Arrange
        var order = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 1,
            Price = price,
            Quantity = 1000
        };

        // Act
        var result = _validator.TestValidate(order);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    [InlineData(500)] // Not multiple of 1000
    [InlineData(1500)] // Not multiple of 1000
    public void Validate_InvalidQuantity_ShouldHaveError(int quantity)
    {
        // Arrange
        var order = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 1,
            Price = 580.00m,
            Quantity = quantity
        };

        // Act
        var result = _validator.TestValidate(order);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(2000)]
    [InlineData(10000)]
    public void Validate_ValidQuantityMultipleOf1000_ShouldNotHaveError(int quantity)
    {
        // Arrange
        var order = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 1,
            Price = 580.00m,
            Quantity = quantity
        };

        // Act
        var result = _validator.TestValidate(order);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }
}

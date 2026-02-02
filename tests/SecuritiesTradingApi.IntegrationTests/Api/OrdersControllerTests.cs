using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.IntegrationTests.Api;

public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_ValidOrder_ReturnsCreated()
    {
        // Arrange
        var createOrder = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 1,
            Price = 580.00m,
            Quantity = 1000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrder);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResultDto>();
        result.Should().NotBeNull();
        result!.OrderId.Should().BeGreaterThan(0);
        result.StockCode.Should().Be("2330");
    }

    [Fact]
    public async Task CreateOrder_InvalidQuantity_ReturnsBadRequest()
    {
        // Arrange - Quantity not multiple of 1000
        var createOrder = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "2330",
            OrderType = 1,
            Price = 580.00m,
            Quantity = 500
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrder);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_NonExistingStock_ReturnsNotFound()
    {
        // Arrange
        var createOrder = new CreateOrderDto
        {
            UserId = 1,
            StockCode = "9999",
            OrderType = 1,
            Price = 100.00m,
            Quantity = 1000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrder);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrder_NonExistingOrder_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/orders/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

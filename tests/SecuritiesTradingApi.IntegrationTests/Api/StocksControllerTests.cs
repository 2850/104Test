using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SecuritiesTradingApi.Models.Dtos;

namespace SecuritiesTradingApi.IntegrationTests.Api;

public class StocksControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StocksControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStockInfo_ExistingStock_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/stocks/2330");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stock = await response.Content.ReadFromJsonAsync<StockInfoDto>();
        stock.Should().NotBeNull();
        stock!.StockCode.Should().Be("2330");
    }

    [Fact]
    public async Task GetStockInfo_NonExistingStock_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/stocks/9999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStockQuote_ExistingStock_ReturnsOkOrServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/api/stocks/2330/quote");

        // Assert - Either success or 503 (TWSE API might be unavailable)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
}

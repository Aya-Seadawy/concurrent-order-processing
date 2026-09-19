using System.Net;
using OrderProcessing.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderProcessing.IntegrationTests;

/// <summary>Required scenario: with one unit in stock, submit two different orders concurrently.</summary>
public class ConcurrentDistinctOrdersTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.SetProductStockAsync("WIDGET-1", 1);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ExactlyOneSucceeds_OneConflicts_StockFinishesAtZero()
    {
        var orderA = _client.CreateOrderAsync("order-a-key", "cust-a", ("WIDGET-1", 1));
        var orderB = _client.CreateOrderAsync("order-b-key", "cust-b", ("WIDGET-1", 1));

        var results = await Task.WhenAll(orderA, orderB);

        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);

        var conflict = results.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("insufficient_stock", conflict.Body.GetString("code"));

        var finalStock = await _factory.GetProductStockAsync("WIDGET-1");
        Assert.Equal(0, finalStock);
    }
}

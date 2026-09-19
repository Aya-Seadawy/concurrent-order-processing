using System.Net;
using OrderProcessing.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderProcessing.IntegrationTests;

/// <summary>Required scenario: reuse a successful key with a changed quantity -> 409, no additional DB changes.</summary>
public class IdempotencyKeyReuseWithDifferentPayloadTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ReusingKeyWithDifferentQuantity_Returns409_AndMakesNoAdditionalChanges()
    {
        const string idempotencyKey = "reuse-key";

        var first = await _client.CreateOrderAsync(idempotencyKey, "cust-1", ("WIDGET-1", 1));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var stockAfterFirst = await _factory.GetProductStockAsync("WIDGET-1");
        var ordersAfterFirst = await _factory.CountOrdersAsync();

        var second = await _client.CreateOrderAsync(idempotencyKey, "cust-1", ("WIDGET-1", 2));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("idempotency_key_conflict", second.Body.GetString("code"));

        Assert.Equal(stockAfterFirst, await _factory.GetProductStockAsync("WIDGET-1"));
        Assert.Equal(ordersAfterFirst, await _factory.CountOrdersAsync());
    }
}

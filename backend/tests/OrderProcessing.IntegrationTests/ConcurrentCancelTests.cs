using System.Net;
using OrderProcessing.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderProcessing.IntegrationTests;

/// <summary>Required scenario: cancel one order concurrently twice -> final status Cancelled, single stock restoration.</summary>
public class ConcurrentCancelTests : IAsyncLifetime
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
    public async Task ConcurrentDoubleCancel_RestoresStockExactlyOnce()
    {
        var stockBeforeOrder = await _factory.GetProductStockAsync("WIDGET-1");

        var created = await _client.CreateOrderAsync("cancel-test-key", "cust-cancel", ("WIDGET-1", 2));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var orderId = created.Body.GetGuid("id");

        var stockAfterOrder = await _factory.GetProductStockAsync("WIDGET-1");
        Assert.Equal(stockBeforeOrder - 2, stockAfterOrder);

        var cancel1 = _client.CancelOrderAsync(orderId);
        var cancel2 = _client.CancelOrderAsync(orderId);
        var results = await Task.WhenAll(cancel1, cancel2);

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.All(results, r => Assert.Equal("Cancelled", r.Body.GetString("status")));

        var finalStock = await _factory.GetProductStockAsync("WIDGET-1");
        Assert.Equal(stockBeforeOrder, finalStock);

        var final = await _client.GetOrderAsync(orderId);
        Assert.Equal("Cancelled", final.Body.GetString("status"));
    }

    [Fact]
    public async Task LaterReplayOfOriginalSubmission_DoesNotCreateOrConfirmAgain()
    {
        var stockBeforeOrder = await _factory.GetProductStockAsync("WIDGET-1");

        // 1. Create order
        var created = await _client.CreateOrderAsync("replay-after-cancel-key", "cust-cancel-replay", ("WIDGET-1", 2));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var orderId = created.Body.GetGuid("id");

        // 2. Cancel order -> stock restored
        var cancel = await _client.CancelOrderAsync(orderId);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Cancelled", cancel.Body.GetString("status"));
        Assert.Equal(stockBeforeOrder, await _factory.GetProductStockAsync("WIDGET-1"));

        var orderCountAfterCancel = await _factory.CountOrdersAsync();

        // 3. Later replay of the original submission with the same idempotency key
        var replay = await _client.CreateOrderAsync("replay-after-cancel-key", "cust-cancel-replay", ("WIDGET-1", 2));

        // Replay must not deduct stock again
        var stockAfterReplay = await _factory.GetProductStockAsync("WIDGET-1");
        Assert.Equal(stockBeforeOrder, stockAfterReplay);

        // Replay must not create a new order
        var orderCountAfterReplay = await _factory.CountOrdersAsync();
        Assert.Equal(orderCountAfterCancel, orderCountAfterReplay);

        // Replay must not confirm the order again (remains Cancelled in database and in replay response)
        Assert.Equal("Cancelled", replay.Body.GetString("status"));
        var inDb = await _client.GetOrderAsync(orderId);
        Assert.Equal("Cancelled", inDb.Body.GetString("status"));
    }
}

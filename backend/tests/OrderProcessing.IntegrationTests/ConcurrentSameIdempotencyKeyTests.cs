using System.Net;
using OrderProcessing.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderProcessing.IntegrationTests;

/// <summary>Required scenario: submit the same order concurrently with the same Idempotency-Key.</summary>
public class ConcurrentSameIdempotencyKeyTests : IAsyncLifetime
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
    public async Task CreatesExactlyOneOrder_OneStockDeduction_OneNotification()
    {
        const string idempotencyKey = "duplicate-key";
        var initialStock = await _factory.GetProductStockAsync("WIDGET-1");

        var requests = Enumerable.Range(0, 8)
            .Select(_ => _client.CreateOrderAsync(idempotencyKey, "cust-dup", ("WIDGET-1", 1)));

        var results = await Task.WhenAll(requests);

        // Every response that isn't a transient "still processing" 409 must describe the very same order.
        var settled = results.Where(r => r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK).ToList();
        Assert.NotEmpty(settled);

        var orderIds = settled.Select(r => r.Body.GetGuid("id")).Distinct().ToList();
        Assert.Single(orderIds);

        Assert.Equal(1, await _factory.CountOrdersAsync());
        Assert.Equal(1, await _factory.CountNotificationsForOrderAsync(orderIds[0]));

        var finalStock = await _factory.GetProductStockAsync("WIDGET-1");
        Assert.Equal(initialStock - 1, finalStock);
    }
}

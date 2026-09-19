using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderProcessing.IntegrationTests;

/// <summary>Required scenario: force a failure before commit -> stock, order and notification changes all roll back.</summary>
public class TransactionRollbackTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory.ConfigureTestServices = services =>
        {
            services.RemoveAll<IChaosHook>();
            services.AddSingleton<IChaosHook, ThrowingChaosHook>();
        };
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
    public async Task FailureBeforeCommit_RollsBackStockOrderAndNotification()
    {
        var stockBefore = await _factory.GetProductStockAsync("WIDGET-1");
        var ordersBefore = await _factory.CountOrdersAsync();
        var idempotencyKeysBefore = await _factory.CountIdempotencyKeysAsync();

        var result = await _client.CreateOrderAsync("chaos-key", "cust-chaos", ("WIDGET-1", 1));

        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);

        Assert.Equal(stockBefore, await _factory.GetProductStockAsync("WIDGET-1"));
        Assert.Equal(ordersBefore, await _factory.CountOrdersAsync());
        Assert.Equal(idempotencyKeysBefore, await _factory.CountIdempotencyKeysAsync());
    }
}

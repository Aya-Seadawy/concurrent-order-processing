using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Infrastructure.Notifications;
using OrderProcessing.Infrastructure.Persistence;
using OrderProcessing.IntegrationTests.Infrastructure;
using Xunit;

namespace OrderProcessing.IntegrationTests;

/// <summary>Required scenario: delivery fails then succeeds (retry works); separately, exhausted attempts -> Failed.</summary>
public class NotificationRetryTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        // A huge poll interval keeps the auto-registered hosted service from racing our manual ProcessOnceAsync
        // calls; zero backoff makes retries immediately eligible without needing to fake the clock.
        _factory.ExtraConfiguration["Notifications:Worker:PollIntervalMs"] = "3600000";
        _factory.ExtraConfiguration["Notifications:Worker:BaseBackoffSeconds"] = "0";
        _factory.ExtraConfiguration["Notifications:Worker:MaxAttempts"] = "3";
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
    public async Task DeliveryFailsTwiceThenSucceeds_NotificationEndsUpSent()
    {
        SetFailUntilAttempt(2);

        var created = await _client.CreateOrderAsync("notif-retry-key", "cust-notif", ("WIDGET-1", 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var orderId = created.Body.GetGuid("id");

        var worker = _factory.Services.GetRequiredService<NotificationDispatcherWorker>();

        // Attempt 1 fails, attempt 2 fails, attempt 3 succeeds (FailUntilAttempt = 2).
        await worker.ProcessOnceAsync(CancellationToken.None);
        Assert.Equal(NotificationStatus.Pending, await GetNotificationStatusAsync(orderId));

        await worker.ProcessOnceAsync(CancellationToken.None);
        Assert.Equal(NotificationStatus.Pending, await GetNotificationStatusAsync(orderId));

        await worker.ProcessOnceAsync(CancellationToken.None);
        Assert.Equal(NotificationStatus.Sent, await GetNotificationStatusAsync(orderId));

        var final = await _client.GetOrderAsync(orderId);
        Assert.Equal("Sent", final.Body.GetString("notificationStatus"));
    }

    [Fact]
    public async Task DeliveryAlwaysFails_ExhaustsRetriesAndEndsUpFailed()
    {
        SetFailUntilAttempt(1000); // never succeeds within MaxAttempts = 3

        var created = await _client.CreateOrderAsync("notif-exhaust-key", "cust-notif-2", ("WIDGET-1", 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var orderId = created.Body.GetGuid("id");

        var worker = _factory.Services.GetRequiredService<NotificationDispatcherWorker>();

        await worker.ProcessOnceAsync(CancellationToken.None);
        await worker.ProcessOnceAsync(CancellationToken.None);
        await worker.ProcessOnceAsync(CancellationToken.None);

        Assert.Equal(NotificationStatus.Failed, await GetNotificationStatusAsync(orderId));

        var final = await _client.GetOrderAsync(orderId);
        Assert.Equal("Failed", final.Body.GetString("notificationStatus"));
    }

    private void SetFailUntilAttempt(int attempts)
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<FakeDeliveryOptions>>();
        // The fake delivery service reads IOptionsMonitor.CurrentValue each call; mutate the bound instance directly.
        options.CurrentValue.FailUntilAttempt = attempts;
    }

    private async Task<NotificationStatus> GetNotificationStatusAsync(Guid orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        var notification = await db.OrderNotifications.AsNoTracking().SingleAsync(n => n.OrderId == orderId);
        return notification.Status;
    }
}

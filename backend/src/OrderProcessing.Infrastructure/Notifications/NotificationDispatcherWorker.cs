using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Application.Common.Models;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Infrastructure.Persistence;

namespace OrderProcessing.Infrastructure.Notifications;

/// <summary>
/// Polls OrderNotifications for pending work. A stale-claim sweep releases InProgress rows whose lease
/// expired (worker crash recovery) back to Pending so they get redispatched — see design-note.md for the
/// resulting at-least-once / duplicate-delivery risk this implies.
/// </summary>
public sealed class NotificationDispatcherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<NotificationWorkerOptions> _options;
    private readonly ILogger<NotificationDispatcherWorker> _logger;

    public NotificationDispatcherWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<NotificationWorkerOptions> options,
        ILogger<NotificationDispatcherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification worker iteration failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.CurrentValue.PollIntervalMs), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Runs a single poll/claim/dispatch cycle; exposed publicly so tests can drive it deterministically.</summary>
    public async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        var delivery = scope.ServiceProvider.GetRequiredService<IDeliveryService>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var options = _options.CurrentValue;
        var now = clock.UtcNow;

        var leaseExpiry = now.AddSeconds(-options.ClaimLeaseSeconds);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE OrderNotifications SET Status = {nameof(NotificationStatus.Pending)}, ClaimedAtUtc = NULL WHERE Status = {nameof(NotificationStatus.InProgress)} AND ClaimedAtUtc <= {leaseExpiry}",
            cancellationToken);

        var candidateIds = await context.OrderNotifications
            .AsNoTracking()
            .Where(n => n.Status == NotificationStatus.Pending && n.NextAttemptAtUtc <= now)
            .OrderBy(n => n.NextAttemptAtUtc)
            .Take(options.BatchSize)
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in candidateIds)
        {
            await ClaimAndDispatchAsync(context, delivery, clock, id, options, cancellationToken);
        }
    }

    private async Task ClaimAndDispatchAsync(
        OrderProcessingDbContext context,
        IDeliveryService delivery,
        IDateTimeProvider clock,
        Guid notificationId,
        NotificationWorkerOptions options,
        CancellationToken cancellationToken)
    {
        var notification = await context.OrderNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null || notification.Status != NotificationStatus.Pending)
            return;

        notification.MarkClaimed(clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);

        var order = await context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == notification.OrderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Notification {NotificationId} references missing order {OrderId}", notification.Id, notification.OrderId);
            notification.MarkFailedAttempt("Order not found.", options.MaxAttempts, clock.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var payload = new NotificationPayload(order.Id, order.CustomerReference, order.TotalAmount);
        var deliveryResult = await delivery.SendAsync(notification.Id, payload, cancellationToken);

        if (deliveryResult.Success)
        {
            notification.MarkSent(clock.UtcNow);
            _logger.LogInformation("Notification {NotificationId} for order {OrderId} delivered", notification.Id, notification.OrderId);
        }
        else
        {
            var backoffSeconds = options.BaseBackoffSeconds * Math.Pow(2, notification.AttemptCount);
            var nextAttempt = clock.UtcNow.AddSeconds(backoffSeconds);
            notification.MarkFailedAttempt(deliveryResult.Error ?? "Unknown delivery failure.", options.MaxAttempts, nextAttempt);

            if (notification.Status == NotificationStatus.Failed)
                _logger.LogWarning("Notification {NotificationId} exhausted retries and is now Failed", notification.Id);
            else
                _logger.LogWarning(
                    "Notification {NotificationId} delivery attempt {Attempt} failed, next retry at {NextAttempt}",
                    notification.Id, notification.AttemptCount, notification.NextAttemptAtUtc);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

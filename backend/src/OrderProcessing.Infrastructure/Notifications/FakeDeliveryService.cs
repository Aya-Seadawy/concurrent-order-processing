using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Infrastructure.Notifications;

/// <summary>Stands in for a real notification channel; failure behavior is deterministic per-event so tests are reliable.</summary>
public sealed class FakeDeliveryService : IDeliveryService
{
    private readonly IOptionsMonitor<FakeDeliveryOptions> _options;
    private readonly ConcurrentDictionary<Guid, int> _attemptsByEvent = new();

    public FakeDeliveryService(IOptionsMonitor<FakeDeliveryOptions> options)
    {
        _options = options;
    }

    public Task<DeliveryResult> SendAsync(Guid eventId, NotificationPayload payload, CancellationToken cancellationToken)
    {
        var attempt = _attemptsByEvent.AddOrUpdate(eventId, 1, (_, current) => current + 1);
        var failUntilAttempt = _options.CurrentValue.FailUntilAttempt;

        if (attempt <= failUntilAttempt)
            return Task.FromResult(new DeliveryResult(false, $"Simulated transient delivery failure on attempt {attempt}."));

        return Task.FromResult(new DeliveryResult(true, null));
    }
}

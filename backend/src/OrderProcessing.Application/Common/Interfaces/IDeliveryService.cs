using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Common.Interfaces;

/// <summary>Implemented by a fake delivery service in Infrastructure; eventId is stable across every retry attempt.</summary>
public interface IDeliveryService
{
    Task<DeliveryResult> SendAsync(Guid eventId, NotificationPayload payload, CancellationToken cancellationToken);
}

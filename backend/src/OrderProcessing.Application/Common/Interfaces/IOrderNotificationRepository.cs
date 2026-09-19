using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Common.Interfaces;

public interface IOrderNotificationRepository
{
    /// <summary>Stages the notification for persistence; call IUnitOfWork.SaveChangesAsync to commit.</summary>
    void Add(OrderNotification notification);

    Task<OrderNotification?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}

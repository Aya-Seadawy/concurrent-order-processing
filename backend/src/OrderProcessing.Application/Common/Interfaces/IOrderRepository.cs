using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Common.Interfaces;

public interface IOrderRepository
{
    /// <summary>Stages the order (and its lines) for persistence; call IUnitOfWork.SaveChangesAsync to commit.</summary>
    void Add(Order order);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderLine>> GetLinesAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Atomic conditional cancel (Confirmed -&gt; Cancelled); returns rows affected (0 = not found or already Cancelled).</summary>
    Task<int> TryCancelAsync(Guid id, DateTime cancelledAtUtc, CancellationToken cancellationToken);
}

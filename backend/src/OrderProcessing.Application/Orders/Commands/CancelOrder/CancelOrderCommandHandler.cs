using MediatR;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Application.Common.Mapping;

namespace OrderProcessing.Application.Orders.Commands.CancelOrder;

/// <summary>
/// The conditional UPDATE ... WHERE Status='Confirmed' is the concurrency guard: only the transaction that
/// actually flips the row gets to restore stock, so concurrent/duplicate cancels restore inventory exactly once.
/// </summary>
public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, CancelOrderResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orders;
    private readonly IOrderNotificationRepository _notifications;
    private readonly IProductRepository _products;
    private readonly IDateTimeProvider _clock;

    public CancelOrderCommandHandler(
        IUnitOfWork unitOfWork,
        IOrderRepository orders,
        IOrderNotificationRepository notifications,
        IProductRepository products,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _orders = orders;
        _notifications = notifications;
        _products = products;
        _clock = clock;
    }

    public async Task<CancelOrderResult> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var rowsAffected = await _orders.TryCancelAsync(request.OrderId, _clock.UtcNow, cancellationToken);

        if (rowsAffected == 1)
        {
            var lines = await _orders.GetLinesAsync(request.OrderId, cancellationToken);
            foreach (var line in lines)
            {
                await _products.RestoreStockAsync(line.ProductCode, line.Quantity, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            var dto = await OrderLoader.LoadAsync(_orders, _notifications, request.OrderId, cancellationToken);
            return new CancelOrderResult { Outcome = CancelOrderOutcome.Cancelled, Order = dto };
        }

        await transaction.RollbackAsync(cancellationToken);
        var existing = await OrderLoader.LoadAsync(_orders, _notifications, request.OrderId, cancellationToken);
        return existing is null
            ? new CancelOrderResult { Outcome = CancelOrderOutcome.NotFound }
            : new CancelOrderResult { Outcome = CancelOrderOutcome.AlreadyCancelled, Order = existing };
    }
}

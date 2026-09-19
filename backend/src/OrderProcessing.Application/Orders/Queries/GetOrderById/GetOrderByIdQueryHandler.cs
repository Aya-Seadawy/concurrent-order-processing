using MediatR;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Application.Common.Mapping;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderNotificationRepository _notifications;

    public GetOrderByIdQueryHandler(IOrderRepository orders, IOrderNotificationRepository notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    public Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return OrderLoader.LoadAsync(_orders, _notifications, request.OrderId, cancellationToken);
    }
}

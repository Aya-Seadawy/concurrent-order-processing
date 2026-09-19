using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Application.Common.Models;

namespace OrderProcessing.Application.Common.Mapping;

/// <summary>Shared "load an order + its notification status as a DTO" query, used by 3 handlers.</summary>
public static class OrderLoader
{
    public static async Task<OrderDto?> LoadAsync(
        IOrderRepository orders,
        IOrderNotificationRepository notifications,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return null;

        var notification = await notifications.GetByOrderIdAsync(orderId, cancellationToken);
        return OrderMapper.ToDto(order, notification);
    }
}

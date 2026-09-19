using OrderProcessing.Application.Common.Models;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.Common.Mapping;

/// <summary>Single source of truth for turning a persisted Order (+ notification) into the API-facing DTO.</summary>
public static class OrderMapper
{
    public static OrderDto ToDto(Order order, OrderNotification? notification)
    {
        return new OrderDto(
            order.Id,
            order.CustomerReference,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAtUtc,
            order.CancelledAtUtc,
            order.Lines.Select(l => new OrderLineDto(l.ProductCode, l.Quantity, l.UnitPriceAtPurchase, l.LineTotal)).ToList(),
            notification?.Status.ToString() ?? NotificationStatus.Pending.ToString());
    }
}

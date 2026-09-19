namespace OrderProcessing.Application.Common.Models;

public sealed record OrderDto(
    Guid Id,
    string CustomerReference,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc,
    IReadOnlyList<OrderLineDto> Lines,
    string NotificationStatus);

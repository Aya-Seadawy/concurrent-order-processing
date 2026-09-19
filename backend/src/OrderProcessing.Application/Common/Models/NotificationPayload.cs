namespace OrderProcessing.Application.Common.Models;

public sealed record NotificationPayload(Guid OrderId, string CustomerReference, decimal TotalAmount);

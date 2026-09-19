namespace OrderProcessing.Application.Common.Models;

public sealed record DeliveryResult(bool Success, string? Error);

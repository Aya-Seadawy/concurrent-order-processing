namespace OrderProcessing.Application.Common.Models;

public sealed record OrderLineDto(string ProductCode, int Quantity, decimal UnitPriceAtPurchase, decimal LineTotal);

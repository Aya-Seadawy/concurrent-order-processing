namespace OrderProcessing.Application.Common.Models;

public sealed record ProductDto(string Code, string Name, decimal UnitPrice, int AvailableQuantity);

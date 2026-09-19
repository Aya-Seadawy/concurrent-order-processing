namespace OrderProcessing.Domain.Entities;

public class OrderLine
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPriceAtPurchase { get; private set; }
    public decimal LineTotal => UnitPriceAtPurchase * Quantity;

    private OrderLine()
    {
    }

    public OrderLine(string productCode, int quantity, decimal unitPriceAtPurchase)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Product code is required.", nameof(productCode));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPriceAtPurchase < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPriceAtPurchase));

        Id = Guid.NewGuid();
        ProductCode = productCode;
        Quantity = quantity;
        UnitPriceAtPurchase = unitPriceAtPurchase;
    }
}

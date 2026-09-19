namespace OrderProcessing.Domain.Entities;

/// <summary>Read-side snapshot only; the atomic stock deduction itself is a raw conditional UPDATE in Infrastructure, not a method here.</summary>
public class Product
{
    public int Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int AvailableQuantity { get; private set; }

    private Product()
    {
    }

    public Product(string code, string name, decimal unitPrice, int availableQuantity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Product code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        if (availableQuantity < 0)
            throw new ArgumentException("Available quantity cannot be negative.", nameof(availableQuantity));

        Code = code;
        Name = name;
        UnitPrice = unitPrice;
        AvailableQuantity = availableQuantity;
    }
}

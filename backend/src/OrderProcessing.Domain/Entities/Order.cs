using OrderProcessing.Domain.Enums;
using OrderProcessing.Domain.Exceptions;

namespace OrderProcessing.Domain.Entities;

public class Order
{
    private readonly List<OrderLine> _lines = new();

    public Guid Id { get; private set; }
    public string CustomerReference { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    private Order()
    {
    }

    public Order(Guid id, string customerReference, IEnumerable<OrderLine> lines, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(customerReference))
            throw new ArgumentException("Customer reference is required.", nameof(customerReference));

        var lineList = lines?.ToList() ?? throw new ArgumentNullException(nameof(lines));
        if (lineList.Count == 0)
            throw new ArgumentException("Order must contain at least one line.", nameof(lines));

        Id = id;
        CustomerReference = customerReference;
        Status = OrderStatus.Confirmed;
        CreatedAtUtc = createdAtUtc;
        _lines.AddRange(lineList);
        TotalAmount = _lines.Sum(l => l.LineTotal);
    }

    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOrderStateException($"Order {Id} cannot be cancelled because it is {Status}.");

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
    }
}

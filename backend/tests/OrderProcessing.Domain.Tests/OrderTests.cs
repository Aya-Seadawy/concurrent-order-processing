using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Domain.Exceptions;
using Xunit;

namespace OrderProcessing.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void Constructor_ComputesTotalFromLines()
    {
        var lines = new[]
        {
            new OrderLine("A", 2, 10m),
            new OrderLine("B", 1, 5m)
        };

        var order = new Order(Guid.NewGuid(), "cust-1", lines, DateTime.UtcNow);

        Assert.Equal(25m, order.TotalAmount);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Constructor_ThrowsWhenNoLines()
    {
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), "cust-1", Array.Empty<OrderLine>(), DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_ThrowsWhenCustomerReferenceMissing()
    {
        var lines = new[] { new OrderLine("A", 1, 10m) };
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), "  ", lines, DateTime.UtcNow));
    }

    [Fact]
    public void Cancel_TransitionsToCancelled()
    {
        var order = new Order(Guid.NewGuid(), "cust-1", new[] { new OrderLine("A", 1, 10m) }, DateTime.UtcNow);
        var cancelledAt = DateTime.UtcNow;

        order.Cancel(cancelledAt);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(cancelledAt, order.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_ThrowsWhenAlreadyCancelled()
    {
        var order = new Order(Guid.NewGuid(), "cust-1", new[] { new OrderLine("A", 1, 10m) }, DateTime.UtcNow);
        order.Cancel(DateTime.UtcNow);

        Assert.Throws<InvalidOrderStateException>(() => order.Cancel(DateTime.UtcNow));
    }
}

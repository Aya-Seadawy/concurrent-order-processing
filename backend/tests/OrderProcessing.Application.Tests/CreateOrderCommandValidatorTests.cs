using OrderProcessing.Application.Orders.Commands.CreateOrder;
using Xunit;

namespace OrderProcessing.Application.Tests;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public void Fails_WhenIdempotencyKeyMissing()
    {
        var command = new CreateOrderCommand("", "cust-1", new[] { new CreateOrderLineRequest("A", 1) });
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Fails_WhenNoLines()
    {
        var command = new CreateOrderCommand("key", "cust-1", Array.Empty<CreateOrderLineRequest>());
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Fails_WhenQuantityNotPositive()
    {
        var command = new CreateOrderCommand("key", "cust-1", new[] { new CreateOrderLineRequest("A", 0) });
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Fails_WhenDuplicateProductCodes()
    {
        var command = new CreateOrderCommand(
            "key", "cust-1", new[] { new CreateOrderLineRequest("A", 1), new CreateOrderLineRequest("a", 2) });

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Succeeds_ForValidCommand()
    {
        var command = new CreateOrderCommand("key", "cust-1", new[] { new CreateOrderLineRequest("A", 1) });
        Assert.True(_validator.Validate(command).IsValid);
    }
}

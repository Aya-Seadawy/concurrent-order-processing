using OrderProcessing.Domain.Entities;
using Xunit;

namespace OrderProcessing.Domain.Tests;

public class OrderLineTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ThrowsWhenQuantityNotPositive(int quantity)
    {
        Assert.Throws<ArgumentException>(() => new OrderLine("A", quantity, 10m));
    }

    [Fact]
    public void Constructor_ThrowsWhenProductCodeMissing()
    {
        Assert.Throws<ArgumentException>(() => new OrderLine("", 1, 10m));
    }

    [Fact]
    public void LineTotal_IsQuantityTimesUnitPrice()
    {
        var line = new OrderLine("A", 3, 9.5m);
        Assert.Equal(28.5m, line.LineTotal);
    }
}

public class ProductTests
{
    [Fact]
    public void Constructor_ThrowsWhenAvailableQuantityNegative()
    {
        Assert.Throws<ArgumentException>(() => new Product("A", "Widget", 10m, -1));
    }

    [Fact]
    public void Constructor_ThrowsWhenUnitPriceNegative()
    {
        Assert.Throws<ArgumentException>(() => new Product("A", "Widget", -1m, 10));
    }
}

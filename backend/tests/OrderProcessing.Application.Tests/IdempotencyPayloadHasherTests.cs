using OrderProcessing.Application.Common.Idempotency;
using Xunit;

namespace OrderProcessing.Application.Tests;

public class IdempotencyPayloadHasherTests
{
    private readonly IdempotencyPayloadHasher _hasher = new();

    [Fact]
    public void ComputeHash_IsOrderInsensitiveForLines()
    {
        var hash1 = _hasher.ComputeHash("cust-1", new[] { ("A", 1), ("B", 2) });
        var hash2 = _hasher.ComputeHash("cust-1", new[] { ("B", 2), ("A", 1) });

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DiffersWhenQuantityChanges()
    {
        var hash1 = _hasher.ComputeHash("cust-1", new[] { ("A", 1) });
        var hash2 = _hasher.ComputeHash("cust-1", new[] { ("A", 2) });

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IsCaseInsensitiveForProductCode()
    {
        var hash1 = _hasher.ComputeHash("cust-1", new[] { ("abc", 1) });
        var hash2 = _hasher.ComputeHash("cust-1", new[] { ("ABC", 1) });

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DiffersWhenCustomerReferenceChanges()
    {
        var hash1 = _hasher.ComputeHash("cust-1", new[] { ("A", 1) });
        var hash2 = _hasher.ComputeHash("cust-2", new[] { ("A", 1) });

        Assert.NotEqual(hash1, hash2);
    }
}

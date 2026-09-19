using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OrderProcessing.Application.Common.Interfaces;

namespace OrderProcessing.Application.Common.Idempotency;

/// <summary>Payload equivalence = same customer reference + same set of (productCode, quantity) pairs, order-insensitive.</summary>
public sealed class IdempotencyPayloadHasher : IIdempotencyPayloadHasher
{
    public string ComputeHash(string customerReference, IReadOnlyCollection<(string ProductCode, int Quantity)> lines)
    {
        var normalizedLines = lines
            .Select(l => new { ProductCode = l.ProductCode.Trim().ToUpperInvariant(), l.Quantity })
            .OrderBy(l => l.ProductCode, StringComparer.Ordinal)
            .ToList();

        var canonicalPayload = JsonSerializer.Serialize(new
        {
            CustomerReference = customerReference.Trim(),
            Lines = normalizedLines
        });

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
        return Convert.ToHexString(hashBytes);
    }
}

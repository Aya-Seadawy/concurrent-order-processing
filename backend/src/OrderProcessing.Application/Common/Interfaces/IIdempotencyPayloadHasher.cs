namespace OrderProcessing.Application.Common.Interfaces;

public interface IIdempotencyPayloadHasher
{
    string ComputeHash(string customerReference, IReadOnlyCollection<(string ProductCode, int Quantity)> lines);
}

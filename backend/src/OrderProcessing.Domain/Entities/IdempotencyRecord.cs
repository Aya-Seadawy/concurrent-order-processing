using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Domain.Entities;

/// <summary>Row existence + unique Key constraint is what serializes concurrent duplicate submissions at the DB level.</summary>
public class IdempotencyRecord
{
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public IdempotencyStatus Status { get; private set; }
    public Guid? OrderId { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private IdempotencyRecord()
    {
    }

    public IdempotencyRecord(string key, string requestHash, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Idempotency key is required.", nameof(key));

        Key = key;
        RequestHash = requestHash;
        Status = IdempotencyStatus.Processing;
        CreatedAtUtc = createdAtUtc;
    }

    public void Complete(int responseStatusCode, string responseBody, Guid? orderId, DateTime completedAtUtc)
    {
        Status = IdempotencyStatus.Completed;
        ResponseStatusCode = responseStatusCode;
        ResponseBody = responseBody;
        OrderId = orderId;
        CompletedAtUtc = completedAtUtc;
    }
}

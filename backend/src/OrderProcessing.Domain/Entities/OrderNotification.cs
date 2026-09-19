using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Domain.Entities;

/// <summary>Id doubles as the stable event id passed to the delivery service across every retry attempt.</summary>
public class OrderNotification
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? ClaimedAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    private OrderNotification()
    {
    }

    public OrderNotification(Guid orderId, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Status = NotificationStatus.Pending;
        AttemptCount = 0;
        NextAttemptAtUtc = createdAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkClaimed(DateTime claimedAtUtc)
    {
        Status = NotificationStatus.InProgress;
        ClaimedAtUtc = claimedAtUtc;
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = sentAtUtc;
        ClaimedAtUtc = null;
    }

    public void MarkFailedAttempt(string error, int maxAttempts, DateTime nextAttemptAtUtc)
    {
        AttemptCount++;
        LastError = error;
        ClaimedAtUtc = null;
        Status = AttemptCount >= maxAttempts ? NotificationStatus.Failed : NotificationStatus.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    /// <summary>Crash recovery: a claim left InProgress past its lease is released back to Pending for re-dispatch.</summary>
    public void ReleaseStaleClaim()
    {
        Status = NotificationStatus.Pending;
        ClaimedAtUtc = null;
    }
}

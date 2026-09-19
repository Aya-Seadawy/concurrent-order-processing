namespace OrderProcessing.Infrastructure.Notifications;

/// <summary>Bound from configuration ("Notifications:FakeDelivery"); lets tests/ops force transient failures.</summary>
public sealed class FakeDeliveryOptions
{
    /// <summary>Simulated delivery fails for every attempt up to and including this attempt number. 0 = never fails.</summary>
    public int FailUntilAttempt { get; set; }
}

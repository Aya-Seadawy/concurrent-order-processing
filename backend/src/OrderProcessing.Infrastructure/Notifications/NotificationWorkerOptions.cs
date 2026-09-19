namespace OrderProcessing.Infrastructure.Notifications;

/// <summary>Bound from configuration ("Notifications:Worker").</summary>
public sealed class NotificationWorkerOptions
{
    public int PollIntervalMs { get; set; } = 1000;
    public int BatchSize { get; set; } = 10;
    public int MaxAttempts { get; set; } = 5;
    public int BaseBackoffSeconds { get; set; } = 2;
    public int ClaimLeaseSeconds { get; set; } = 30;
}

namespace OrderProcessing.Domain.Enums;

/// <summary>Processing is only ever transient within a single DB transaction; a crash rolls the claim back entirely.</summary>
public enum IdempotencyStatus
{
    Processing,
    Completed
}

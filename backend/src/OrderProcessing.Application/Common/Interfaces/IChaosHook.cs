namespace OrderProcessing.Application.Common.Interfaces;

/// <summary>No-op in production; integration tests override this to force a failure before the order transaction commits.</summary>
public interface IChaosHook
{
    Task BeforeOrderCommitAsync(CancellationToken cancellationToken);
}

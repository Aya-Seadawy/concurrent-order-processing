using OrderProcessing.Application.Common.Interfaces;

namespace OrderProcessing.IntegrationTests.Infrastructure;

/// <summary>Deterministic replacement for the no-op chaos hook: forces the create-order transaction to fail before commit.</summary>
public sealed class ThrowingChaosHook : IChaosHook
{
    public Task BeforeOrderCommitAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Injected chaos failure for test.");
}

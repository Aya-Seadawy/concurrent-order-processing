using OrderProcessing.Application.Common.Interfaces;

namespace OrderProcessing.Application.Common.ChaosHook;

public sealed class NoOpChaosHook : IChaosHook
{
    public Task BeforeOrderCommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

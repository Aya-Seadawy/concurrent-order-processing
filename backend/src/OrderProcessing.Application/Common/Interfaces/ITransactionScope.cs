namespace OrderProcessing.Application.Common.Interfaces;

/// <summary>An open database transaction; committing/rolling back is explicit, disposal without commit rolls back.</summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}

using Microsoft.EntityFrameworkCore.Storage;
using OrderProcessing.Application.Common.Interfaces;

namespace OrderProcessing.Infrastructure.Persistence;

internal sealed class EfTransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _transaction;

    public EfTransactionScope(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken) => _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}

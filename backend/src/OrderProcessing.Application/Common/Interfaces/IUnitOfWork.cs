namespace OrderProcessing.Application.Common.Interfaces;

/// <summary>Owns the transaction boundary and the "flush pending changes" operation for the current request.</summary>
public interface IUnitOfWork
{
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

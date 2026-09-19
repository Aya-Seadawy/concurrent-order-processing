using OrderProcessing.Application.Common.Interfaces;

namespace OrderProcessing.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly OrderProcessingDbContext _context;

    public EfUnitOfWork(OrderProcessingDbContext context)
    {
        _context = context;
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransactionScope(transaction);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}

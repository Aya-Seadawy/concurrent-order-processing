using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyRecordRepository : IIdempotencyRecordRepository
{
    private readonly OrderProcessingDbContext _context;

    public IdempotencyRecordRepository(OrderProcessingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryInsertClaimAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        _context.IdempotencyRecords.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation on Key: keep the DB-specific exception type out of Application.
            _context.Entry(record).State = EntityState.Detached;
            return false;
        }
    }

    public Task<IdempotencyRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return _context.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
    }
}

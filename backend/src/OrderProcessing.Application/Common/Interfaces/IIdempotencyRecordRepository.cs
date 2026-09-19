using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Common.Interfaces;

public interface IIdempotencyRecordRepository
{
    /// <summary>
    /// Immediately attempts to persist the claim (does not wait for IUnitOfWork.SaveChangesAsync).
    /// Returns false if a row with this Key already exists — the unique constraint on Key is what
    /// serializes concurrent duplicate submissions; the DB-specific exception never leaves this repository.
    /// </summary>
    Task<bool> TryInsertClaimAsync(IdempotencyRecord record, CancellationToken cancellationToken);

    Task<IdempotencyRecord?> GetByKeyAsync(string key, CancellationToken cancellationToken);
}

using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyDictionary<string, Product>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Atomic conditional deduction; returns rows affected (0 = insufficient stock, no change made).</summary>
    Task<int> TryDeductStockAsync(string code, int quantity, CancellationToken cancellationToken);

    Task RestoreStockAsync(string code, int quantity, CancellationToken cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly OrderProcessingDbContext _context;

    public ProductRepository(OrderProcessingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<string, Product>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => codes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        return products.ToDictionary(p => p.Code);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Products.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<int> TryDeductStockAsync(string code, int quantity, CancellationToken cancellationToken)
    {
        return _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Products SET AvailableQuantity = AvailableQuantity - {quantity} WHERE Code = {code} AND AvailableQuantity >= {quantity}",
            cancellationToken);
    }

    public Task RestoreStockAsync(string code, int quantity, CancellationToken cancellationToken)
    {
        return _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Products SET AvailableQuantity = AvailableQuantity + {quantity} WHERE Code = {code}",
            cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderProcessingDbContext _context;

    public OrderRepository(OrderProcessingDbContext context)
    {
        _context = context;
    }

    public void Add(Order order) => _context.Orders.Add(order);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderLine>> GetLinesAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _context.OrderLines
            .AsNoTracking()
            .Where(l => l.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }

    public Task<int> TryCancelAsync(Guid id, DateTime cancelledAtUtc, CancellationToken cancellationToken)
    {
        return _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Orders SET Status = {nameof(OrderStatus.Cancelled)}, CancelledAtUtc = {cancelledAtUtc} WHERE Id = {id} AND Status = {nameof(OrderStatus.Confirmed)}",
            cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class OrderNotificationRepository : IOrderNotificationRepository
{
    private readonly OrderProcessingDbContext _context;

    public OrderNotificationRepository(OrderProcessingDbContext context)
    {
        _context = context;
    }

    public void Add(OrderNotification notification) => _context.OrderNotifications.Add(notification);

    public Task<OrderNotification?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return _context.OrderNotifications
            .AsNoTracking()
            .SingleOrDefaultAsync(n => n.OrderId == orderId, cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Infrastructure.Persistence;

namespace OrderProcessing.IntegrationTests.Infrastructure;

public static class DbTestHelpers
{
    public static async Task SetProductStockAsync(this TestWebApplicationFactory factory, string code, int quantity)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Products SET AvailableQuantity = {quantity} WHERE Code = {code}");
    }

    public static async Task<int> GetProductStockAsync(this TestWebApplicationFactory factory, string code)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        return (await db.Products.AsNoTracking().SingleAsync(p => p.Code == code)).AvailableQuantity;
    }

    public static async Task<int> CountOrdersAsync(this TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        return await db.Orders.CountAsync();
    }

    public static async Task<int> CountNotificationsForOrderAsync(this TestWebApplicationFactory factory, Guid orderId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        return await db.OrderNotifications.CountAsync(n => n.OrderId == orderId);
    }

    public static async Task<int> CountIdempotencyKeysAsync(this TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
        return await db.IdempotencyRecords.CountAsync();
    }
}

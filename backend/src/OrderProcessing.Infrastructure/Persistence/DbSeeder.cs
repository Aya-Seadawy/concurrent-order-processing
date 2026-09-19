using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence;

public static class DbSeeder
{
    /// <summary>Tolerates a concurrent seed race (e.g. WebApplicationFactory probing the host twice) via a unique-index catch.</summary>
    public static async Task SeedAsync(OrderProcessingDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Products.AnyAsync(cancellationToken))
            return;

        context.Products.AddRange(
            new Product("WIDGET-1", "Standard Widget", 19.99m, 10),
            new Product("GADGET-1", "Deluxe Gadget", 49.50m, 5));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another concurrent startup already seeded the same database; nothing left to do.
        }
    }
}

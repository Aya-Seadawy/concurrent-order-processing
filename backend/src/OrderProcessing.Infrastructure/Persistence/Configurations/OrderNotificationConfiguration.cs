using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence.Configurations;

public sealed class OrderNotificationConfiguration : IEntityTypeConfiguration<OrderNotification>
{
    public void Configure(EntityTypeBuilder<OrderNotification> builder)
    {
        builder.ToTable("OrderNotifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.OrderId).IsRequired();
        builder.HasIndex(n => n.OrderId);

        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.AttemptCount).IsRequired();
        builder.Property(n => n.NextAttemptAtUtc).IsRequired();
        builder.Property(n => n.CreatedAtUtc).IsRequired();
        builder.Property(n => n.LastError).HasMaxLength(1000);

        // Supports the crash-recovery sweep that reclaims stale InProgress rows past their lease.
        builder.HasIndex(n => new { n.Status, n.NextAttemptAtUtc });
    }
}

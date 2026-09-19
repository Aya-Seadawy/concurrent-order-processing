using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence.Configurations;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderId).IsRequired();
        builder.Property(l => l.ProductCode).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.UnitPriceAtPurchase).IsRequired();

        builder.Ignore(l => l.LineTotal);
    }
}

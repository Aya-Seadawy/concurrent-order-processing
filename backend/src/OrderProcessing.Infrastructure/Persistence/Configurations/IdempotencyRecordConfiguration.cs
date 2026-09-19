using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyKeys");

        // The primary key IS the concurrency-control mechanism: a second concurrent INSERT with the same
        // Key is rejected (or blocks then rejects, on SQLite's single-writer model) by the DB engine itself.
        builder.HasKey(r => r.Key);
        builder.Property(r => r.Key).HasMaxLength(200);

        builder.Property(r => r.RequestHash).IsRequired().HasMaxLength(128);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.ResponseBody).HasColumnType("TEXT");
        builder.Property(r => r.CreatedAtUtc).IsRequired();
    }
}

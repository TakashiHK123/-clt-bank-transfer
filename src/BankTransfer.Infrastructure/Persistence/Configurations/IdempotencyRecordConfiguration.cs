using BankTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankTransfer.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> b)
    {
        b.ToTable("idempotency");

        b.HasKey(x => x.Key);

        b.Property(x => x.Key)
            .HasMaxLength(100);

        b.Property(x => x.RequestHash)
            .IsRequired()
            .HasMaxLength(128);

        b.Property(x => x.ResponseJson)
            .IsRequired();

        b.Property(x => x.CreatedAt)
            .IsRequired();
    }
}

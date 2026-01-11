using BankTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankTransfer.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> b)
    {
        b.ToTable("transfers");

        b.HasKey(x => x.Id);

        b.Property(x => x.Amount)
            .HasPrecision(18, 2);

        b.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        b.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

    }
}

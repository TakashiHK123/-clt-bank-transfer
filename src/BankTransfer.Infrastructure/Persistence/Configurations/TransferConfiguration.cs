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

        b.Property(x => x.FromAccountId).IsRequired();
        b.Property(x => x.ToAccountId).IsRequired();

        b.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        b.Property(x => x.CreatedAt).IsRequired();

        b.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        b.HasIndex(x => new { x.FromAccountId, x.IdempotencyKey })
            .IsUnique();

        b.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
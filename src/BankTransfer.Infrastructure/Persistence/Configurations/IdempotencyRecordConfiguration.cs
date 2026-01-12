using BankTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankTransfer.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> b)
    {
        b.ToTable("idempotency_records");
        b.HasKey(x => x.Id);

        b.Property(x => x.AccountId).IsRequired();
        b.Property(x => x.TransferId).IsRequired();

        b.Property(x => x.Key).IsRequired().HasMaxLength(200);
        b.Property(x => x.RequestHash).IsRequired().HasMaxLength(200);
        b.Property(x => x.ResponseJson).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();

        b.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Transfer)
            .WithOne(t => t.IdempotencyRecord)
            .HasForeignKey<IdempotencyRecord>(x => x.TransferId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.AccountId, x.Key }).IsUnique();

        b.HasIndex(x => x.TransferId).IsUnique();

        b.HasIndex(x => x.AccountId);
    }
}
using BankTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankTransfer.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("accounts");

        b.HasKey(x => x.Id);

        b.Property(x => x.UserId)
            .IsRequired();

        b.Property(x => x.Name).IsRequired()
            .HasMaxLength(150);

        b.Property(x => x.Balance)
            .HasPrecision(18, 2);
        
        b.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        b.Property(x => x.Version)
            .IsConcurrencyToken();
        
        b.HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(a => a.UserId);
    }
}

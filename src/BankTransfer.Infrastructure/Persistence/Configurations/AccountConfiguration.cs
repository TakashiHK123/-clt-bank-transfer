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

        b.Property(x => x.Name).IsRequired()
            .HasMaxLength(150);

        b.Property(x => x.Balance)
            .HasPrecision(18, 2);

        b.Property(x => x.Version)
            .IsConcurrencyToken();
    }
}

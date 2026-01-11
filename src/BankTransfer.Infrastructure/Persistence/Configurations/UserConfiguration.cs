using BankTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankTransfer.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");

        b.HasKey(x => x.Id);

        b.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(80);

        b.HasIndex(x => x.Username)
            .IsUnique();

        b.Property(x => x.PasswordHash)
            .IsRequired();

        b.Property(x => x.AccountId)
            .IsRequired();
    }
}

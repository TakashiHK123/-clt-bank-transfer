using BankTransfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.Infrastructure.Persistence;

public sealed class BankTransferDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<User> Users => Set<User>();
    
    public BankTransferDbContext(DbContextOptions<BankTransferDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(b =>
        {
            b.ToTable("accounts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(150);
            b.Property(x => x.Balance).HasPrecision(18, 2);
            b.Property(x => x.Version).IsConcurrencyToken(); // clave para evitar doble transacciones
        });

        modelBuilder.Entity<Transfer>(b =>
        {
            b.ToTable("transfers");
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(100);
            b.HasIndex(x => x.IdempotencyKey).IsUnique(); // 1 transferencia por idempotencyKey
        });

        modelBuilder.Entity<IdempotencyRecord>(b =>
        {
            b.ToTable("idempotency");
            b.HasKey(x => x.Key);
            b.Property(x => x.Key).HasMaxLength(100);
            b.Property(x => x.RequestHash).IsRequired().HasMaxLength(128);
            b.Property(x => x.ResponseJson).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
        });
        
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Username).IsRequired().HasMaxLength(80);
            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.AccountId).IsRequired();
        });
    }
}

public sealed class IdempotencyRecord
{
    public string Key { get; set; } = default!;
    public string RequestHash { get; set; } = default!;
    public string ResponseJson { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
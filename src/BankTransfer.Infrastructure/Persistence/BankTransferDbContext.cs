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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankTransferDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}

using BankTransfer.Application.Abstractions;
using BankTransfer.Infrastructure.Persistence;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BankTransferDbContext _db;

    public UnitOfWork(BankTransferDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
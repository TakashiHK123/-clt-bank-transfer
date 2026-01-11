using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly BankTransferDbContext _db;

    public AccountRepository(BankTransferDbContext db) => _db = db;

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
    public Task<List<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    => _db.Accounts
        .Where(a => a.UserId == userId)
        .ToListAsync(ct);

    public Task<Account?> GetByIdForUserAsync(Guid accountId, Guid userId, CancellationToken ct)
    => _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct);

    public Task<List<Account>> ListByUserAsync(Guid userId, CancellationToken ct)
        => _db.Accounts.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(ct);
    public void Update(Account account) => _db.Accounts.Update(account);
}
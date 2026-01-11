using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class TransferRepository : ITransferRepository
{
    private readonly BankTransferDbContext _db;

    public TransferRepository(BankTransferDbContext db) => _db = db;

    public Task AddAsync(Transfer transfer, CancellationToken ct)
        => _db.Transfers.AddAsync(transfer, ct).AsTask();

    public async Task<List<Transfer>> GetHistoryByAccountIdAsync(Guid accountId, CancellationToken ct)
    {
        var list = await _db.Transfers
            .AsNoTracking()
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .ToListAsync(ct);

        return list
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
    }
}
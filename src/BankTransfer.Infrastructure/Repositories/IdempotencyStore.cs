using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly BankTransferDbContext _db;

    public IdempotencyStore(BankTransferDbContext db) => _db = db;

    public async Task<IdempotencyResult?> GetAsync(Guid ownerId, string key, CancellationToken ct)
    {
        var record = await _db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == ownerId && x.Key == key, ct);

        return record is null
            ? null
            : new IdempotencyResult(record.RequestHash, record.ResponseJson);
    }

    public Task SaveSuccessAsync(
        Guid ownerId,
        Guid transferId,
        string key,
        string requestHash,
        string responseJson,
        CancellationToken ct)
    {
        _db.IdempotencyRecords.Add(new IdempotencyRecord(
            accountId: ownerId,
            transferId: transferId,
            key: key,
            requestHash: requestHash,
            responseJson: responseJson
        ));

        return Task.CompletedTask;
    }
}
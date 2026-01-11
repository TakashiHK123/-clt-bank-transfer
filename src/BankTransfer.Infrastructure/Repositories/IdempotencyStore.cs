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
            .FirstOrDefaultAsync(x => x.OwnerId == ownerId && x.Key == key, ct);

        return record is null ? null : new IdempotencyResult(record.RequestHash, record.ResponseJson);
    }

    public async Task SaveSuccessAsync(Guid ownerId, string key, string requestHash, string responseJson, CancellationToken ct)
    {
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            OwnerId = ownerId,
            Key = key,
            RequestHash = requestHash,
            ResponseJson = responseJson
        });

        await _db.SaveChangesAsync(ct);
    }
}
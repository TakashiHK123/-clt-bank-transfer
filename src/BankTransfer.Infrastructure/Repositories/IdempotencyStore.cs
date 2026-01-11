using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly BankTransferDbContext _db;

    public IdempotencyStore(BankTransferDbContext db) => _db = db;

    public async Task<IdempotencyResult?> GetAsync(string key, CancellationToken ct)
    {
        var record = await _db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, ct);

        return record is null ? null : new IdempotencyResult(record.RequestHash, record.ResponseJson);
    }

    public async Task SaveSuccessAsync(string key, string requestHash, string responseJson, CancellationToken ct)
    {
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            RequestHash = requestHash,
            ResponseJson = responseJson
        });

        await _db.SaveChangesAsync(ct);
    }
}
using System.Data;
using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Queries;
using BankTransfer.Infrastructure.DTOs;
using Dapper;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly IDbConnection _connection;

    public IdempotencyStore(IDbConnection connection) => _connection = connection;

    public async Task<IdempotencyResult?> GetAsync(Guid ownerId, string key, CancellationToken ct)
    {
        var record = await _connection.QuerySingleOrDefaultAsync<IdempotencyRecordDto>(
            IdempotencyQueries.Get, 
            new { OwnerId = ownerId.ToString(), Key = key });

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
        return _connection.ExecuteAsync(IdempotencyQueries.SaveSuccess, new
        {
            Id = Guid.NewGuid().ToString(),
            AccountId = ownerId.ToString(),
            TransferId = transferId.ToString(),
            Key = key,
            RequestHash = requestHash,
            ResponseJson = responseJson,
            CreatedAt = DateTime.UtcNow.ToString("O")
        });
    }
}
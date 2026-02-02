using System.Data;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Queries;
using Dapper;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class TransferRepository : ITransferRepository
{
    private readonly IDbConnection _connection;

    public TransferRepository(IDbConnection connection) => _connection = connection;

    public Task AddAsync(Transfer transfer, CancellationToken ct)
        => _connection.ExecuteAsync(TransferQueries.Add, new 
        { 
            transfer.Id, 
            transfer.FromAccountId, 
            transfer.ToAccountId, 
            transfer.Amount, 
            transfer.CreatedAt 
        });

    public Task<List<Transfer>> GetHistoryByAccountIdAsync(Guid accountId, CancellationToken ct)
        => _connection.QueryAsync<Transfer>(TransferQueries.GetHistoryByAccountId, new { AccountId = accountId })
            .ContinueWith(t => t.Result.ToList(), ct);
}
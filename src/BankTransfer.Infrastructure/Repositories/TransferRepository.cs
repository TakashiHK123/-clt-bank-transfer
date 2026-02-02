using System.Data;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Queries;
using BankTransfer.Infrastructure.DTOs;
using BankTransfer.Infrastructure.Extensions;
using Dapper;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class TransferRepository : ITransferRepository
{
    private readonly IDbConnection _connection;

    public TransferRepository(IDbConnection connection) => _connection = connection;

    public Task AddAsync(Transfer transfer, CancellationToken ct)
        => _connection.ExecuteAsync(TransferQueries.Add, new 
        { 
            Id = transfer.Id.ToString(),
            FromAccountId = transfer.FromAccountId.ToString(),
            ToAccountId = transfer.ToAccountId.ToString(),
            transfer.Amount, 
            CreatedAt = transfer.CreatedAt.ToString("O")
        });

    public async Task<List<Transfer>> GetHistoryByAccountIdAsync(Guid accountId, CancellationToken ct)
    {
        var dtos = await _connection.QueryAsync<TransferDto>(TransferQueries.GetHistoryByAccountId, 
            new { AccountId = accountId.ToString() });
        
        return dtos.Select(dto => TransferExtensions.CreateFromDto(
            Guid.Parse(dto.Id),
            Guid.Parse(dto.FromAccountId),
            Guid.Parse(dto.ToAccountId),
            dto.Amount,
            DateTimeOffset.Parse(dto.CreatedAt)
        )).ToList();
    }
}
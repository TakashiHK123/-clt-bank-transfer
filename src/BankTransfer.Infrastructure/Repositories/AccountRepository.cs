using System.Data;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Queries;
using BankTransfer.Infrastructure.DTOs;
using Dapper;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IDbConnection _connection;

    public AccountRepository(IDbConnection connection) => _connection = connection;

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var dto = await _connection.QuerySingleOrDefaultAsync<AccountDto>(AccountQueries.GetById, new { Id = id.ToString() });
        return dto == null ? null : MapToEntity(dto);
    }

    public async Task<List<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var dtos = await _connection.QueryAsync<AccountDto>(AccountQueries.GetByUserId, new { UserId = userId.ToString() });
        return dtos.Select(MapToEntity).ToList();
    }

    public async Task<Account?> GetByIdForUserAsync(Guid accountId, Guid userId, CancellationToken ct)
    {
        var dto = await _connection.QuerySingleOrDefaultAsync<AccountDto>(AccountQueries.GetByIdForUser, 
            new { AccountId = accountId.ToString(), UserId = userId.ToString() });
        return dto == null ? null : MapToEntity(dto);
    }

    public async Task<List<Account>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        var dtos = await _connection.QueryAsync<AccountDto>(AccountQueries.ListByUser, new { UserId = userId.ToString() });
        return dtos.Select(MapToEntity).ToList();
    }

    public Task UpdateAsync(Account account, CancellationToken ct = default)
        => _connection.ExecuteAsync(AccountQueries.Update, new { account.Balance, Id = account.Id.ToString() });

    private static Account MapToEntity(AccountDto dto)
    {
        return Account.Seed(
            Guid.Parse(dto.Id),
            Guid.Parse(dto.UserId),
            dto.Name,
            dto.Balance,
            dto.Currency
        );
    }
}
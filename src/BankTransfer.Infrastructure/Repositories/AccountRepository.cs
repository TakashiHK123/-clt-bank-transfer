using System.Data;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Queries;
using Dapper;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IDbConnection _connection;

    public AccountRepository(IDbConnection connection) => _connection = connection;

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct)
        => _connection.QuerySingleOrDefaultAsync<Account>(AccountQueries.GetById, new { Id = id });

    public Task<List<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => _connection.QueryAsync<Account>(AccountQueries.GetByUserId, new { UserId = userId })
            .ContinueWith(t => t.Result.ToList(), ct);

    public Task<Account?> GetByIdForUserAsync(Guid accountId, Guid userId, CancellationToken ct)
        => _connection.QuerySingleOrDefaultAsync<Account>(AccountQueries.GetByIdForUser, 
            new { AccountId = accountId, UserId = userId });

    public Task<List<Account>> ListByUserAsync(Guid userId, CancellationToken ct)
        => _connection.QueryAsync<Account>(AccountQueries.ListByUser, new { UserId = userId })
            .ContinueWith(t => t.Result.ToList(), ct);

    public Task UpdateAsync(Account account, CancellationToken ct = default)
        => _connection.ExecuteAsync(AccountQueries.Update, new { account.Balance, account.Id });
}
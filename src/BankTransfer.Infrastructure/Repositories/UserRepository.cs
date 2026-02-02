using System.Data;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Queries;
using Dapper;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection) => _connection = connection;

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
        => _connection.QuerySingleOrDefaultAsync<User>(UserQueries.GetByUsername, new { Username = username });
}
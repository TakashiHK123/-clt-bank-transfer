using System.Data;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Queries;
using BankTransfer.Infrastructure.DTOs;
using Dapper;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection) => _connection = connection;

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        var dto = await _connection.QuerySingleOrDefaultAsync<UserDto>(UserQueries.GetByUsername, new { Username = username });
        return dto == null ? null : new User(dto.Username, dto.PasswordHash, Guid.Parse(dto.Id));
    }
}
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly BankTransferDbContext _db;

    public UserRepository(BankTransferDbContext db) => _db = db;

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        var u = username.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(x => x.Username == u, ct);
    }
}
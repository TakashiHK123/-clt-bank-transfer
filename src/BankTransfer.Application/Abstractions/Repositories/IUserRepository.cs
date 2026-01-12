using BankTransfer.Domain.Entities;

namespace BankTransfer.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
}
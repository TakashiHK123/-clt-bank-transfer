using BankTransfer.Domain.Entities;

namespace BankTransfer.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
}
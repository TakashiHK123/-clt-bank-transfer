using BankTransfer.Domain.Entities;

namespace BankTransfer.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Account?> GetByIdForUserAsync(Guid accountId, Guid userId, CancellationToken ct);
    Task<List<Account>> ListByUserAsync(Guid userId, CancellationToken ct);
    void Update(Account account);
}
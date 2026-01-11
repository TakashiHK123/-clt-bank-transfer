using BankTransfer.Domain.Entities;

namespace BankTransfer.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Account>> ListAsync(CancellationToken ct);
    void Update(Account account);
}
using BankTransfer.Application.DTOs;

namespace BankTransfer.Application.Abstractions.Services;

public interface IAccountService
{
    Task<AccountDto?> GetByIdForUserAsync(Guid accountId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<AccountWithIdDto>> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
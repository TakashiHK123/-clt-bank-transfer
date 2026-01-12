using BankTransfer.Application.Abstractions;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.Abstractions.Services;
using BankTransfer.Application.DTOs;

namespace BankTransfer.Application.Services;

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _accounts;

    public AccountService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<AccountDto?> GetByIdForUserAsync(Guid accountId, Guid userId, CancellationToken ct)
    {
        var acc = await _accounts.GetByIdForUserAsync(accountId, userId, ct);
        if (acc is null) return null;

        return new AccountDto(
            acc.Name,
            acc.Balance,
            acc.Currency
        );
    }

    public async Task<IReadOnlyList<AccountWithIdDto>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var list = await _accounts.GetByUserIdAsync(userId, ct);

        return list.Select(a => new AccountWithIdDto(
            a.Id,
            a.Name,
            a.Balance,
            a.Currency
        )).ToList();
    }
}
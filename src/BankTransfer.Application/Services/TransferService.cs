using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.Abstractions.Services;
using BankTransfer.Application.DTOs;

namespace BankTransfer.Application.Services;

public sealed class TransferService : ITransferService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransferRepository _transfers;
    private readonly TransferFundsService _transferFunds;

    public TransferService(
        IAccountRepository accounts,
        ITransferRepository transfers,
        TransferFundsService transferFunds)
    {
        _accounts = accounts;
        _transfers = transfers;
        _transferFunds = transferFunds;
    }

    public Task<TransferResponseDto> CreateAsync(
        Guid userId,
        TransferRequestDto request,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        return _transferFunds.ExecuteAsync(userId, request, idempotencyKey.ToString(), ct);
    }

    public async Task<IReadOnlyList<TransferHistoryItemDto>?> GetHistoryByAccountAsync(
        Guid userId,
        Guid accountId,
        CancellationToken ct)
    {
        var acc = await _accounts.GetByIdForUserAsync(accountId, userId, ct);
        if (acc is null)
            return null; 

        var list = await _transfers.GetHistoryByAccountIdAsync(accountId, ct);

        return list.Select(t => new TransferHistoryItemDto(
            t.Id,
            t.FromAccountId,
            t.ToAccountId,
            t.Amount,
            t.Currency,
            t.CreatedAt,
            t.FromAccountId == accountId ? "OUT" : "IN"
        )).ToList();
    }
}
using BankTransfer.Application.DTOs;

namespace BankTransfer.Application.Abstractions.Services;


public interface ITransferService
{
    Task<TransferResponseDto> CreateAsync(Guid userId, TransferRequestDto request, Guid idempotencyKey, CancellationToken ct);
    Task<IReadOnlyList<TransferHistoryItemDto>?> GetHistoryByAccountAsync(Guid userId, Guid accountId, CancellationToken ct);
}
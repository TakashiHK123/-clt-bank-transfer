namespace BankTransfer.Application.DTOs;

public sealed record TransferHistoryItemDto(
    Guid Id,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt,
    string Direction
);
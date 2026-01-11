namespace BankTransfer.Application.DTOs;

public sealed record TransferRequestDto(Guid FromAccountId, Guid ToAccountId, decimal Amount);
public sealed record TransferResponseDto(Guid TransferId, Guid FromAccountId, Guid ToAccountId, decimal Amount, DateTimeOffset CreatedAt);
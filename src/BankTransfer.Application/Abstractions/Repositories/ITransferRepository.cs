using BankTransfer.Domain.Entities;

namespace BankTransfer.Application.Abstractions.Repositories;

public interface ITransferRepository
{
    Task AddAsync(Transfer transfer, CancellationToken ct);
    Task<List<Transfer>> GetHistoryByAccountIdAsync(Guid accountId, CancellationToken ct);
}
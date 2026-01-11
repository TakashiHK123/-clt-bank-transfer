using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankTransfer.Application.Abstractions;
using BankTransfer.Application.DTOs;
using BankTransfer.Domain.Entities;
using BankTransfer.Domain.Exceptions;

namespace BankTransfer.Application.UseCases;

public sealed class TransferFundsUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly ITransferRepository _transfers;
    private readonly IIdempotencyStore _idempotency;
    private readonly IUnitOfWork _uow;

    public TransferFundsUseCase(
        IAccountRepository accounts,
        ITransferRepository transfers,
        IIdempotencyStore idempotency,
        IUnitOfWork uow)
    {
        _accounts = accounts;
        _transfers = transfers;
        _idempotency = idempotency;
        _uow = uow;
    }

    public async Task<TransferResponseDto> ExecuteAsync(TransferRequestDto request, string idempotencyKey, CancellationToken ct)
    {
        var requestHash = ComputeHash($"{request.FromAccountId}|{request.ToAccountId}|{request.Amount}");
        
        var cached = await _idempotency.GetAsync(idempotencyKey, ct);
        if (cached is not null)
        {
            if (!string.Equals(cached.RequestHash, requestHash, StringComparison.Ordinal))
                throw new IdempotencyConflictException(idempotencyKey);

            return JsonSerializer.Deserialize<TransferResponseDto>(cached.ResponseJson)!
                   ?? throw new InvalidOperationException("Stored idempotency response is invalid.");
        }
        
        var from = await _accounts.GetByIdAsync(request.FromAccountId, ct)
                   ?? throw new AccountNotFoundException(request.FromAccountId);

        var to = await _accounts.GetByIdAsync(request.ToAccountId, ct)
                 ?? throw new AccountNotFoundException(request.ToAccountId);

        from.Debit(request.Amount);
        to.Credit(request.Amount);
        
        var transfer = new Transfer(request.FromAccountId, request.ToAccountId, request.Amount, idempotencyKey);
        await _transfers.AddAsync(transfer, ct);

        _accounts.Update(from);
        _accounts.Update(to);

        await _uow.SaveChangesAsync(ct);

        var response = new TransferResponseDto(
            transfer.Id, transfer.FromAccountId, transfer.ToAccountId, transfer.Amount, transfer.CreatedAt);
        
        var responseJson = JsonSerializer.Serialize(response);
        await _idempotency.SaveSuccessAsync(idempotencyKey, requestHash, responseJson, ct);

        return response;
    }

    private static string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

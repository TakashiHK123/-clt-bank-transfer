using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.Abstractions;
using BankTransfer.Application.DTOs;
using BankTransfer.Domain.Entities;
using BankTransfer.Domain.Exceptions;

namespace BankTransfer.Application.Services;

public sealed class TransferFundsService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransferRepository _transfers;
    private readonly IIdempotencyStore _idempotency;
    private readonly IUnitOfWork _uow;

    public TransferFundsService(
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

    public async Task<TransferResponseDto> ExecuteAsync(
        Guid userId,
        TransferRequestDto request,
        string idempotencyKey,
        CancellationToken ct)
    {
        // Dueño de la idempotencia: el usuario autenticado
        var ownerId = userId;

        var requestHash = ComputeHash($"{request.FromAccountId}|{request.ToAccountId}|{request.Amount}");

        // Idempotencia
        var cached = await _idempotency.GetAsync(ownerId, idempotencyKey, ct);
        if (cached is not null)
        {
            if (!string.Equals(cached.RequestHash, requestHash, StringComparison.Ordinal))
                throw new IdempotencyConflictException(idempotencyKey);

            return JsonSerializer.Deserialize<TransferResponseDto>(cached.ResponseJson)!
                   ?? throw new InvalidOperationException("Stored idempotency response is invalid.");
        }

        // Ownership: la cuenta origen debe ser del user
        var from = await _accounts.GetByIdForUserAsync(request.FromAccountId, userId, ct)
                   ?? throw new AccountNotFoundException(request.FromAccountId);

        // La cuenta destino puede ser de otro user (si querés permitir transferencias a terceros)
        var to = await _accounts.GetByIdAsync(request.ToAccountId, ct)
                 ?? throw new AccountNotFoundException(request.ToAccountId);

        // Regla: misma moneda
        if (!string.Equals(from.Currency, to.Currency, StringComparison.OrdinalIgnoreCase))
            throw new CurrencyMismatchException(from.Currency, to.Currency);

        // Aplicar negocio
        from.Debit(request.Amount);
        to.Credit(request.Amount);

        var transfer = new Transfer(request.FromAccountId, request.ToAccountId, request.Amount, from.Currency, idempotencyKey);
        await _transfers.AddAsync(transfer, ct);

        _accounts.Update(from);
        _accounts.Update(to);

        var response = new TransferResponseDto(
            transfer.Id, transfer.FromAccountId, transfer.ToAccountId, transfer.Amount, transfer.CreatedAt);

        var responseJson = JsonSerializer.Serialize(response);

        // Importante: idealmente SaveSuccessAsync NO debería hacer SaveChanges() internamente.
        await _idempotency.SaveSuccessAsync(ownerId, idempotencyKey, requestHash, responseJson, ct);

        // Un solo commit (lo ideal)
        await _uow.SaveChangesAsync(ct);

        return response;
    }

    private static string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

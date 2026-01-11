using System.Security.Claims;
using BankTransfer.Application.Abstractions;
using BankTransfer.Application.DTOs;
using BankTransfer.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";

    private readonly ITransferRepository _transfers;
    private readonly IAccountRepository _accounts;

    public TransfersController(ITransferRepository transfers, IAccountRepository accounts)
    {
        _transfers = transfers;
        _accounts = accounts;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromServices] TransferFundsUseCase useCase,
        [FromBody] TransferRequestDto request,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { message = $"{IdempotencyHeaderName} header is required" });

        if (!Guid.TryParse(idempotencyKey, out _))
            return BadRequest(new { message = $"{IdempotencyHeaderName} must be a valid GUID" });

        // Ahora el token representa al USER, no a la account
        var userIdStr =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Token sin userId válido (claim)." });

        // El usecase valida que FromAccountId sea del user (ownership)
        var result = await useCase.ExecuteAsync(userId, request, idempotencyKey, ct);

        return Created($"/api/transfers/{result.TransferId}", result);
    }

    // Historial por cuenta (porque un usuario puede tener múltiples cuentas)
    [HttpGet("by-account/{accountId:guid}")]
    public async Task<IActionResult> HistoryByAccount(Guid accountId, CancellationToken ct)
    {
        var userIdStr =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Token sin userId válido (claim)." });

        // ownership check: la cuenta consultada debe ser del user
        var acc = await _accounts.GetByIdForUserAsync(accountId, userId, ct);
        if (acc is null)
            return NotFound(); // (mejor que Forbid para no filtrar existencia de IDs)

        var list = await _transfers.GetHistoryByAccountIdAsync(accountId, ct);

        var result = list.Select(t => new
        {
            t.Id,
            t.FromAccountId,
            t.ToAccountId,
            t.Amount,
            t.Currency,
            t.CreatedAt,
            direction = t.FromAccountId == accountId ? "OUT" : "IN"
        });

        return Ok(result); 
    }
}

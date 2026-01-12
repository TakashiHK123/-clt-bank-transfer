using BankTransfer.Api.Auth;
using BankTransfer.Application.Abstractions.Services;
using BankTransfer.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private readonly ITransferService _transfers;

    public TransfersController(ITransferService transfers)
    {
        _transfers = transfers;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] TransferRequestDto request,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { message = $"{IdempotencyHeaderName} header is required" });

        if (!Guid.TryParse(idempotencyKey, out var idempoGuid))
            return BadRequest(new { message = $"{IdempotencyHeaderName} must be a valid GUID" });

        if (!User.TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin userId válido (claim)." });

        var result = await _transfers.CreateAsync(userId, request, idempoGuid, ct);
        
        return Created($"/api/transfers/{result.TransferId}", result);
    }

    [HttpGet("by-account/{accountId:guid}")]
    public async Task<IActionResult> HistoryByAccount(Guid accountId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin userId válido (claim)." });

        var list = await _transfers.GetHistoryByAccountAsync(userId, accountId, ct);

        if (list is null)
            return NotFound();

        return Ok(list);
    }
}

using BankTransfer.Application.DTOs;
using BankTransfer.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BankTransfer.Application.Abstractions;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private readonly ITransferRepository _transfers;
    

    public TransfersController(ITransferRepository transfers)
    {
        _transfers = transfers;
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

        var accountIdClaim = User.FindFirst("accountId")?.Value;

        if (string.IsNullOrWhiteSpace(accountIdClaim))
            return Forbid();

        if (!Guid.TryParse(accountIdClaim, out var myAccountId))
            return Forbid(); 

        if (request.FromAccountId != myAccountId)
            return Forbid();

        var result = await useCase.ExecuteAsync(request, idempotencyKey, ct);

        //return CreatedAtAction(nameof(GetById), new { id = result.TransferId }, result);
        return Created($"/api/transfers/{result.TransferId}", result);
    }
    
    [HttpGet("me")]
    public async Task<IActionResult> Transfers(CancellationToken ct)
    {
        var accountIdStr =
            User.FindFirst("accountId")?.Value ??
            User.FindFirst("AccountId")?.Value ??
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
        if (!Guid.TryParse(accountIdStr, out var myAccountId))
            return Unauthorized(new { message = "Token sin accountId válido (claim)." });
        
        var list = await _transfers.GetHistoryByAccountIdAsync(myAccountId, ct);
        
        var result = list.Select(t => new
        {
            t.Id,
            t.FromAccountId,
            t.ToAccountId,
            t.Amount,
            t.CreatedAt,
            direction = t.FromAccountId == myAccountId ? "OUT" : "IN"
        });
    
        return Ok(result);
    }
    
}
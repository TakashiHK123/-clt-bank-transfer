using BankTransfer.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountRepository _accounts;
    public AccountsController(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdStr =
            User.FindFirst("userId")?.Value ??
            User.FindFirst("UserId")?.Value ??
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdStr, out userId);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var tokenUserId))
            return Unauthorized(new { message = "Token sin userId válido (claim sub/NameIdentifier)." });

        var acc = await _accounts.GetByIdForUserAsync(id, tokenUserId, ct);

        if (acc is null) return NotFound();

        return Ok(new { acc.Name, amount = acc.Balance, currency = acc.Currency });
    }
    
    [HttpGet("me")]
    public async Task<IActionResult> MyAccounts(CancellationToken ct)
    {
        if (!TryGetUserId(out var tokenUserId))
            return Unauthorized(new { message = "Token sin userId válido (claim sub/NameIdentifier)." });

        var list = await _accounts.GetByUserIdAsync(tokenUserId, ct);

        var result = list.Select(a => new
        {
            a.Id,
            a.Name,
            amount = a.Balance,
            currency = a.Currency
        });

        return Ok(result);
    }

}
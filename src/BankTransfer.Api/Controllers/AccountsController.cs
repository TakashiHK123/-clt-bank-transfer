using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankTransfer.Api.Auth;
using BankTransfer.Application.Abstractions.Services;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accounts;
    public AccountsController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var tokenUserId))
            return Unauthorized(new { message = "Token sin userId válido (claim sub/NameIdentifier)." });

        var acc = await _accounts.GetByIdForUserAsync(id, tokenUserId, ct);
        if (acc is null) return NotFound();
        
        return Ok(acc);
    }
    
    [HttpGet("me")]
    public async Task<IActionResult> MyAccounts(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var tokenUserId))
            return Unauthorized(new { message = "Token sin userId válido (claim sub/NameIdentifier)." });

        var list = await _accounts.GetByUserIdAsync(tokenUserId, ct);
        
        return Ok(list);
    }

}
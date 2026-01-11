using BankTransfer.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountRepository _accounts;
    private readonly ITransferRepository _transfers;

    public AccountsController(IAccountRepository accounts, ITransferRepository transfers)
    {
        _accounts = accounts;
        _transfers = transfers;
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var acc = await _accounts.GetByIdAsync(id, ct);
        if (acc is null) return NotFound(new { message = "Account not found" });

        return Ok(new { acc.Name, amount = acc.Balance });
    }
    
    [HttpGet("{id:guid}/transfers")]
    [AllowAnonymous]
    public async Task<IActionResult> TransfersByAccountId(Guid id, CancellationToken ct)
    {
        var list = await _transfers.GetHistoryByAccountIdAsync(id, ct);

        var result = list.Select(t => new
        {
            t.Id,
            t.FromAccountId,
            t.ToAccountId,
            t.Amount,
            t.CreatedAt,
            direction = t.FromAccountId == id ? "OUT" : "IN"
        });

        return Ok(result);
    }
    
}
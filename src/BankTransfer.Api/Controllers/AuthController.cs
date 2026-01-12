using BankTransfer.Application.Abstractions.Services;
using BankTransfer.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] LoginRequestDto req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);

        return Ok(new { access_token = result.AccessToken });
    }
}
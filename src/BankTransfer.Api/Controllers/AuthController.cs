using BankTransfer.Application.Abstractions;
using BankTransfer.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BankTransfer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly TokenService _tokens;

    public AuthController(IUserRepository users, IPasswordHasher hasher, TokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(req.Username, ct);
        if (user is null) return Unauthorized(new { message = "Invalid credentials" });

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var token = _tokens.CreateToken(user.Id, user.Username);
        return Ok(new { access_token = token });
    }

    public sealed record LoginRequest(string Username, string Password);
}
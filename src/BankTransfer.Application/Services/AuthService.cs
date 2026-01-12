using BankTransfer.Application.Abstractions;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.Abstractions.Services;
using BankTransfer.Application.DTOs;

namespace BankTransfer.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto req, CancellationToken ct)
    {
        var user = await _users.GetByUsernameAsync(req.Username, ct);
        if (user is null) throw new UnauthorizedAccessException("Invalid credentials");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = _tokens.CreateToken(user.Id, user.Username);
        return new TokenResponseDto(token);
    }
}
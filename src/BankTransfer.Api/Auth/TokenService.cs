using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankTransfer.Application.Abstractions.Services;
using Microsoft.IdentityModel.Tokens;

namespace BankTransfer.Api.Auth;

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(Guid userId, string username)
    {
        var key = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Missing configuration: Jwt:Key");

        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];

        var expiresMinutesStr = _config["Jwt:ExpiresMinutes"];
        var expiresMinutes = 60;
        if (int.TryParse(expiresMinutesStr, out var parsed) && parsed > 0)
            expiresMinutes = parsed;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),

            new("userId", userId.ToString()),
            new("username", username)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
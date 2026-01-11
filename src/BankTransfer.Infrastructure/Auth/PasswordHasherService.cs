using BankTransfer.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace BankTransfer.Infrastructure.Auth;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
        => _hasher.HashPassword(new object(), password);

    public bool Verify(string password, string passwordHash)
        => _hasher.VerifyHashedPassword(new object(), passwordHash, password)
           == PasswordVerificationResult.Success;
}
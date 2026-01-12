using BankTransfer.Application.DTOs;

namespace BankTransfer.Application.Abstractions.Services;

public interface IAuthService
{
    Task<TokenResponseDto> LoginAsync(LoginRequestDto req, CancellationToken ct);
}
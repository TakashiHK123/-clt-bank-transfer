namespace BankTransfer.Application.Abstractions.Services;

public interface ITokenService
{
    string CreateToken(Guid userId, string username);
}
namespace BankTransfer.Application.DTOs;

public sealed record LoginRequestDto(
    string Username, 
    string Password
);

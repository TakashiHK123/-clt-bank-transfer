namespace BankTransfer.Application.DTOs;

public record AccountDto(
    string Name,
    decimal Amount,
    string Currency
);
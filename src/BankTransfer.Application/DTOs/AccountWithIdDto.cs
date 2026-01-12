namespace BankTransfer.Application.DTOs;

public sealed record AccountWithIdDto(
    Guid Id,
    string Name,
    decimal Amount,
    string Currency
) : AccountDto(Name, Amount, Currency);
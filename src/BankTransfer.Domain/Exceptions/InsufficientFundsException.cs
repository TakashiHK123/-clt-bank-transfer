namespace BankTransfer.Domain.Exceptions;

public sealed class InsufficientFundsException : Exception
{
    public Guid AccountId { get; }
    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientFundsException(Guid accountId, decimal currentBalance, decimal requestedAmount)
        : base($"Insufficient funds. Account={accountId}, Balance={currentBalance}, Requested={requestedAmount}.")
    {
        AccountId = accountId;
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }
}

public sealed class AccountNotFoundException : Exception
{
    public Guid AccountId { get; }
    public AccountNotFoundException(Guid accountId)
        : base($"Account not found. Account={accountId}.")
    {
        AccountId = accountId;
    }
}

public sealed class SameAccountTransferException : Exception
{
    public Guid AccountId { get; }
    public SameAccountTransferException(Guid accountId)
        : base("FromAccountId and ToAccountId cannot be the same.")
    {
        AccountId = accountId;
    }
}

public sealed class IdempotencyConflictException : Exception
{
    public string Key { get; }
    public IdempotencyConflictException(string key)
        : base($"Idempotency key conflict for key='{key}'. Same key used with different request payload.")
    {
        Key = key;
    }
}
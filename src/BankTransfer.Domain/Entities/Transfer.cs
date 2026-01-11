namespace BankTransfer.Domain.Entities;

public sealed class Transfer
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid FromAccountId { get; private set; }
    public Guid ToAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string IdempotencyKey { get; private set; } = default!;

    private Transfer() { } 

    public Transfer(Guid fromAccountId, Guid toAccountId, decimal amount, string idempotencyKey)
    {
        if (fromAccountId == Guid.Empty) throw new ArgumentException("FromAccountId is required.");
        if (toAccountId == Guid.Empty) throw new ArgumentException("ToAccountId is required.");
        if (fromAccountId == toAccountId) throw new Exceptions.SameAccountTransferException(fromAccountId);
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("IdempotencyKey is required.");

        FromAccountId = fromAccountId;
        ToAccountId = toAccountId;
        Amount = amount;
        IdempotencyKey = idempotencyKey.Trim();
    }
}
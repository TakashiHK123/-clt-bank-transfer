namespace BankTransfer.Domain.Entities;

public sealed class IdempotencyRecord
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = default!;
    public Guid TransferId { get; private set; }
    public Transfer Transfer { get; private set; } = default!;
    public string Key { get; private set; } = default!;
    public string RequestHash { get; private set; } = default!;
    public string ResponseJson { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private IdempotencyRecord() { } 

    public IdempotencyRecord(Guid accountId, Guid transferId, string key, string requestHash, string responseJson)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId is required.", nameof(accountId));
        if (transferId == Guid.Empty) throw new ArgumentException("TransferId is required.", nameof(transferId));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(requestHash)) throw new ArgumentException("RequestHash is required.", nameof(requestHash));
        if (string.IsNullOrWhiteSpace(responseJson)) throw new ArgumentException("ResponseJson is required.", nameof(responseJson));

        AccountId = accountId;
        TransferId = transferId;
        Key = key.Trim();
        RequestHash = requestHash.Trim();
        ResponseJson = responseJson;
        CreatedAtUtc = DateTime.UtcNow;
    }
}

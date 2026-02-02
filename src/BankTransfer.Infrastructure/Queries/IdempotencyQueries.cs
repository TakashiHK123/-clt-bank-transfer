namespace BankTransfer.Infrastructure.Queries;

public static class IdempotencyQueries
{
    public const string Get = @"
        SELECT AccountId, TransferId, Key, RequestHash, ResponseJson, CreatedAt
        FROM IdempotencyRecords 
        WHERE AccountId = @OwnerId AND Key = @Key";

    public const string SaveSuccess = @"
        INSERT INTO IdempotencyRecords (Id, AccountId, TransferId, Key, RequestHash, ResponseJson, CreatedAt)
        VALUES (@Id, @AccountId, @TransferId, @Key, @RequestHash, @ResponseJson, @CreatedAt)";
}

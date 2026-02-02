namespace BankTransfer.Infrastructure.Queries;

public static class TransferQueries
{
    public const string Add = @"
        INSERT INTO Transfers (Id, FromAccountId, ToAccountId, Amount, CreatedAt)
        VALUES (@Id, @FromAccountId, @ToAccountId, @Amount, @CreatedAt)";

    public const string GetHistoryByAccountId = @"
        SELECT Id, FromAccountId, ToAccountId, Amount, CreatedAt
        FROM Transfers 
        WHERE FromAccountId = @AccountId OR ToAccountId = @AccountId
        ORDER BY CreatedAt DESC";
}

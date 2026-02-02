namespace BankTransfer.Infrastructure.Queries;

public static class AccountQueries
{
    public const string GetById = @"
        SELECT Id, AccountNumber, Balance, UserId, CreatedAt 
        FROM Accounts 
        WHERE Id = @Id";

    public const string GetByUserId = @"
        SELECT Id, AccountNumber, Balance, UserId, CreatedAt 
        FROM Accounts 
        WHERE UserId = @UserId";

    public const string GetByIdForUser = @"
        SELECT Id, AccountNumber, Balance, UserId, CreatedAt 
        FROM Accounts 
        WHERE Id = @AccountId AND UserId = @UserId";

    public const string ListByUser = @"
        SELECT Id, AccountNumber, Balance, UserId, CreatedAt 
        FROM Accounts 
        WHERE UserId = @UserId";

    public const string Update = @"
        UPDATE Accounts 
        SET Balance = @Balance 
        WHERE Id = @Id";
}

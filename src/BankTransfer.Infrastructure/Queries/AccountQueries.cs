namespace BankTransfer.Infrastructure.Queries;

public static class AccountQueries
{
    public const string GetById = @"
        SELECT Id, Name, Balance, UserId, Currency, Version 
        FROM Accounts 
        WHERE Id = @Id";

    public const string GetByUserId = @"
        SELECT Id, Name, Balance, UserId, Currency, Version 
        FROM Accounts 
        WHERE UserId = @UserId";

    public const string GetByIdForUser = @"
        SELECT Id, Name, Balance, UserId, Currency, Version 
        FROM Accounts 
        WHERE Id = @AccountId AND UserId = @UserId";

    public const string ListByUser = @"
        SELECT Id, Name, Balance, UserId, Currency, Version 
        FROM Accounts 
        WHERE UserId = @UserId";

    public const string Update = @"
        UPDATE Accounts 
        SET Balance = @Balance 
        WHERE Id = @Id";
}

namespace BankTransfer.Infrastructure.Queries;

public static class UserQueries
{
    public const string GetByUsername = @"
        SELECT Id, Username, PasswordHash, CreatedAt
        FROM Users 
        WHERE LOWER(TRIM(Username)) = LOWER(TRIM(@Username))";
}

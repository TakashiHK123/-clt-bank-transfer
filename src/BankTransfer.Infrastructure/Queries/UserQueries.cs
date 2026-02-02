namespace BankTransfer.Infrastructure.Queries;

public static class UserQueries
{
    public const string GetByUsername = @"
        SELECT Id, Username, PasswordHash
        FROM Users 
        WHERE LOWER(TRIM(Username)) = LOWER(TRIM(@Username))";
}

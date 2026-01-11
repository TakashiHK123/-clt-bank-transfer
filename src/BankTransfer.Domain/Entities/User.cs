namespace BankTransfer.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    
    public Guid AccountId { get; private set; }

    private User() { } 

    public User(string username, string passwordHash, Guid accountId)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash required.");
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId required.");

        Username = username.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        AccountId = accountId;
    }
}
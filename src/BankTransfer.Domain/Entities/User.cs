namespace BankTransfer.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public List<Account> Accounts { get; private set; } = new();

    private User() { } 

    public User(string username, string passwordHash, Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));

        Id = id;
        Username = username.Trim();
        PasswordHash = passwordHash;
    }
}
namespace BankTransfer.Domain.Entities;
using System.Linq;


public sealed class Account
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "PYG";
    public long Version { get; private set; } = 0;

    private Account() { } // EF

    public Account(Guid userId, string name, decimal initialBalance, string currency = "PYG")
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (initialBalance < 0) throw new ArgumentOutOfRangeException(nameof(initialBalance));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        var cur = currency.Trim();
        if (cur.Length != 3 || !cur.All(char.IsLetter))
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));

        UserId = userId;
        Name = name.Trim();
        Balance = initialBalance;
        Currency = cur.ToUpperInvariant();
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Balance < amount) throw new Exceptions.InsufficientFundsException(Id, Balance, amount);

        Balance -= amount;
        Version++;
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Balance += amount;
        Version++;
    }

    public static Account Seed(Guid id, Guid userId, string name, decimal initialBalance, string currency = "PYG")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));

        var acc = new Account(userId, name, initialBalance, currency);
        acc.Id = id;
        return acc;
    }

}

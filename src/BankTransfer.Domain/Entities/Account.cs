namespace BankTransfer.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }

    public long Version { get; private set; } = 0;

    private Account() { } 

    public Account(string name, decimal initialBalance)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (initialBalance < 0) throw new ArgumentOutOfRangeException(nameof(initialBalance));

        Name = name.Trim();
        Balance = initialBalance;
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
}
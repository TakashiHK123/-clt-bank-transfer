using System.Data;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Dapper;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class TransferRepositoryTest
{
    private static async Task<IDbConnection> CrearDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            CREATE TABLE Users (
                Id TEXT PRIMARY KEY,
                Username TEXT NOT NULL,
                PasswordHash TEXT NOT NULL
            )");

        await conn.ExecuteAsync(@"
            CREATE TABLE Accounts (
                Id TEXT PRIMARY KEY,
                UserId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Balance REAL NOT NULL,
                Currency TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (UserId) REFERENCES Users(Id)
            )");

        await conn.ExecuteAsync(@"
            CREATE TABLE Transfers (
                Id TEXT PRIMARY KEY,
                FromAccountId TEXT NOT NULL,
                ToAccountId TEXT NOT NULL,
                Amount REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (FromAccountId) REFERENCES Accounts(Id),
                FOREIGN KEY (ToAccountId) REFERENCES Accounts(Id)
            )");

        return conn;
    }

    private static async Task<(User U1, User U2, Account A1, Account A2, Account A3)> SeedUsuariosYCuentasAsync(IDbConnection conn)
    {
        var u1Id = Guid.NewGuid();
        var u2Id = Guid.NewGuid();

        var u1 = new User("user1", "hash", u1Id);
        var u2 = new User("user2", "hash", u2Id);

        var a1 = Account.Seed(Guid.NewGuid(), u1Id, "A1", 100m, "PYG");
        var a2 = Account.Seed(Guid.NewGuid(), u1Id, "A2", 200m, "PYG");
        var a3 = Account.Seed(Guid.NewGuid(), u2Id, "A3", 300m, "PYG");

        // Insert users
        await conn.ExecuteAsync(@"
            INSERT INTO Users (Id, Username, PasswordHash)
            VALUES (@Id, @Username, @PasswordHash)",
            new[] { 
                new { u1.Id, u1.Username, u1.PasswordHash },
                new { u2.Id, u2.Username, u2.PasswordHash }
            });

        // Insert accounts
        await conn.ExecuteAsync(@"
            INSERT INTO Accounts (Id, UserId, Name, Balance, Currency, Version)
            VALUES (@Id, @UserId, @Name, @Balance, @Currency, @Version)",
            new[] {
                new { a1.Id, a1.UserId, a1.Name, a1.Balance, a1.Currency, a1.Version },
                new { a2.Id, a2.UserId, a2.Name, a2.Balance, a2.Currency, a2.Version },
                new { a3.Id, a3.UserId, a3.Name, a3.Balance, a3.Currency, a3.Version }
            });

        return (u1, u2, a1, a2, a3);
    }

    [Fact]
    public async Task AddAsync_CuandoSeAgregaTransferencia_DebeGuardarEnBaseDeDatos()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var (u1, u2, a1, a2, a3) = await SeedUsuariosYCuentasAsync(conn);

        var transfer = new Transfer(a1.Id, a2.Id, 50m, "PYG", "key1");

        var repo = new TransferRepository(conn);

        await repo.AddAsync(transfer, CancellationToken.None);

        // Verify transfer was saved
        var saved = await conn.QuerySingleOrDefaultAsync<Transfer>(@"
            SELECT Id, FromAccountId, ToAccountId, Amount, CreatedAt
            FROM Transfers WHERE Id = @Id", new { transfer.Id });

        Assert.NotNull(saved);
        Assert.Equal(transfer.Id, saved!.Id);
        Assert.Equal(a1.Id, saved.FromAccountId);
        Assert.Equal(a2.Id, saved.ToAccountId);
        Assert.Equal(50m, saved.Amount);
    }

    [Fact]
    public async Task GetHistoryByAccountIdAsync_CuandoHayTransferencias_DebeRetornarHistorialOrdenadoPorFecha()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var (u1, u2, a1, a2, a3) = await SeedUsuariosYCuentasAsync(conn);

        var t1 = new Transfer(a1.Id, a2.Id, 10m, "PYG", "k1");
        var t2 = new Transfer(a2.Id, a1.Id, 20m, "PYG", "k2");
        var t3 = new Transfer(a3.Id, a1.Id, 30m, "PYG", "k3");

        // Insert transfers with different timestamps
        await Task.Delay(10);
        await conn.ExecuteAsync(@"
            INSERT INTO Transfers (Id, FromAccountId, ToAccountId, Amount, CreatedAt)
            VALUES (@Id, @FromAccountId, @ToAccountId, @Amount, @CreatedAt)",
            new { t1.Id, t1.FromAccountId, t1.ToAccountId, t1.Amount, CreatedAt = t1.CreatedAt.ToString("O") });

        await Task.Delay(10);
        await conn.ExecuteAsync(@"
            INSERT INTO Transfers (Id, FromAccountId, ToAccountId, Amount, CreatedAt)
            VALUES (@Id, @FromAccountId, @ToAccountId, @Amount, @CreatedAt)",
            new { t2.Id, t2.FromAccountId, t2.ToAccountId, t2.Amount, CreatedAt = t2.CreatedAt.ToString("O") });

        await Task.Delay(10);
        await conn.ExecuteAsync(@"
            INSERT INTO Transfers (Id, FromAccountId, ToAccountId, Amount, CreatedAt)
            VALUES (@Id, @FromAccountId, @ToAccountId, @Amount, @CreatedAt)",
            new { t3.Id, t3.FromAccountId, t3.ToAccountId, t3.Amount, CreatedAt = t3.CreatedAt.ToString("O") });

        var repo = new TransferRepository(conn);

        var history = await repo.GetHistoryByAccountIdAsync(a1.Id, CancellationToken.None);

        Assert.Equal(3, history.Count);
        
        // Should be ordered by CreatedAt DESC (most recent first)
        Assert.True(history[0].CreatedAt >= history[1].CreatedAt);
        Assert.True(history[1].CreatedAt >= history[2].CreatedAt);
    }
}

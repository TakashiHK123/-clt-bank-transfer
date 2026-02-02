using System.Data;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Dapper;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class AccountRepositoryTest
{
    private static async Task<IDbConnection> CrearDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        // Create tables
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

        return conn;
    }

    private static async Task SeedUsuarioYCuentaAsync(IDbConnection conn, User user, params Account[] accounts)
    {
        await conn.ExecuteAsync(@"
            INSERT INTO Users (Id, Username, PasswordHash)
            VALUES (@Id, @Username, @PasswordHash)",
            new { user.Id, user.Username, user.PasswordHash });

        foreach (var acc in accounts)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO Accounts (Id, UserId, Name, Balance, Currency, Version)
                VALUES (@Id, @UserId, @Name, @Balance, @Currency, @Version)",
                new { acc.Id, acc.UserId, acc.Name, acc.Balance, acc.Currency, acc.Version });
        }
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExisteCuenta_DebeRetornarCuenta()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, userId, "Caja", 100m, "PYG");

        await SeedUsuarioYCuentaAsync(conn, user, acc);

        var repo = new AccountRepository(conn);

        var result = await repo.GetByIdAsync(accountId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(accountId, result!.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Caja", result.Name);
        Assert.Equal(100m, result.Balance);
        Assert.Equal("PYG", result.Currency);
    }

    [Fact]
    public async Task GetByIdAsync_CuandoNoExisteCuenta_DebeRetornarNull()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var repo = new AccountRepository(conn);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_CuandoHayVariasCuentas_DebeRetornarSoloLasDelUsuario()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var user1 = new User("user1", "hash", user1Id);
        var user2 = new User("user2", "hash", user2Id);

        var a1 = Account.Seed(Guid.NewGuid(), user1Id, "U1-A1", 10m, "PYG");
        var a2 = Account.Seed(Guid.NewGuid(), user1Id, "U1-A2", 20m, "PYG");
        var b1 = Account.Seed(Guid.NewGuid(), user2Id, "U2-B1", 30m, "PYG");

        await SeedUsuarioYCuentaAsync(conn, user1, a1, a2);
        await SeedUsuarioYCuentaAsync(conn, user2, b1);

        var repo = new AccountRepository(conn);

        var list = await repo.GetByUserIdAsync(user1Id, CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.All(list, x => Assert.Equal(user1Id, x.UserId));
        Assert.Contains(list, x => x.Id == a1.Id);
        Assert.Contains(list, x => x.Id == a2.Id);
    }

    [Fact]
    public async Task GetByIdForUserAsync_CuandoCuentaPerteneceAlUsuario_DebeRetornarCuenta()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, userId, "Caja", 100m, "PYG");

        await SeedUsuarioYCuentaAsync(conn, user, acc);

        var repo = new AccountRepository(conn);

        var result = await repo.GetByIdForUserAsync(accountId, userId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(accountId, result!.Id);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByIdForUserAsync_CuandoCuentaNoPerteneceAlUsuario_DebeRetornarNull()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var user1 = new User("user1", "hash", user1Id);
        var user2 = new User("user2", "hash", user2Id);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, user1Id, "Caja", 100m, "PYG");

        await SeedUsuarioYCuentaAsync(conn, user1, acc);
        await SeedUsuarioYCuentaAsync(conn, user2);

        var repo = new AccountRepository(conn);

        var result = await repo.GetByIdForUserAsync(accountId, user2Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListByUserAsync_CuandoSeLlama_DebeRetornarCuentasDelUsuario()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var a1 = Account.Seed(Guid.NewGuid(), userId, "A1", 10m, "PYG");
        var a2 = Account.Seed(Guid.NewGuid(), userId, "A2", 20m, "PYG");

        await SeedUsuarioYCuentaAsync(conn, user, a1, a2);

        var repo = new AccountRepository(conn);

        var list = await repo.ListByUserAsync(userId, CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.All(list, x => Assert.Equal(userId, x.UserId));
    }

    [Fact]
    public async Task UpdateAsync_CuandoSeActualizaCuenta_DebePersistirCambios()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, userId, "Caja", 100m, "PYG");

        await SeedUsuarioYCuentaAsync(conn, user, acc);

        var repo = new AccountRepository(conn);

        // Modify balance
        acc.Credit(50m);

        await repo.UpdateAsync(acc);

        // Verify update
        var updated = await repo.GetByIdAsync(accountId, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(150m, updated!.Balance);
    }
}

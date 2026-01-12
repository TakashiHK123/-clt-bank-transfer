using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class IdempotencyStoreTest
{
    private static async Task<(SqliteConnection Conn, BankTransferDbContext Db)> CrearDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        var options = new DbContextOptionsBuilder<BankTransferDbContext>()
            .UseSqlite(conn)
            .Options;

        var db = new BankTransferDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return (conn, db);
    }

    private static async Task<(User User1, User User2, Account Owner, Account Other, Transfer Transfer)> SeedAsync(BankTransferDbContext db)
    {
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var user1 = new User("user1", "hash", user1Id);
        var user2 = new User("user2", "hash", user2Id);

        var owner = Account.Seed(Guid.NewGuid(), user1Id, "OWNER", 1000m, "PYG");
        var other = Account.Seed(Guid.NewGuid(), user2Id, "OTHER", 100m, "PYG");

        var transfer = new Transfer(
            fromAccountId: owner.Id,
            toAccountId: other.Id,
            amount: 10m,
            currency: "PYG",
            idempotencyKey: "seed-key");

        db.Users.AddRange(user1, user2);
        db.Accounts.AddRange(owner, other);
        db.Transfers.Add(transfer);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return (user1, user2, owner, other, transfer);
    }

    [Fact]
    public async Task GetAsync_CuandoNoExisteRegistro_DebeRetornarNull()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var store = new IdempotencyStore(db);

        var result = await store.GetAsync(Guid.NewGuid(), "k1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveSuccessAsync_CuandoSeGuarda_DebeCrearRegistroEnBase()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, owner, _, transfer) = await SeedAsync(db);

        var store = new IdempotencyStore(db);

        var key = "k1";
        var requestHash = "HASH";
        var responseJson = "{\"ok\":true}";

        await store.SaveSuccessAsync(
            ownerId: owner.Id,
            transferId: transfer.Id,
            key: key,
            requestHash: requestHash,
            responseJson: responseJson,
            ct: CancellationToken.None);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var record = await db.IdempotencyRecords
            .FirstOrDefaultAsync(x => x.AccountId == owner.Id && x.Key == key);

        Assert.NotNull(record);
        Assert.Equal(owner.Id, record!.AccountId);
        Assert.Equal(transfer.Id, record.TransferId);
        Assert.Equal(key, record.Key);
        Assert.Equal(requestHash, record.RequestHash);
        Assert.Equal(responseJson, record.ResponseJson);
    }

    [Fact]
    public async Task GetAsync_CuandoExisteRegistro_DebeRetornarIdempotencyResultConHashYJson()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, owner, _, transfer) = await SeedAsync(db);

        var store = new IdempotencyStore(db);

        var key = "k1";
        var requestHash = "HASH-ABC";
        var responseJson = "{\"id\":123}";

        await store.SaveSuccessAsync(owner.Id, transfer.Id, key, requestHash, responseJson, CancellationToken.None);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await store.GetAsync(owner.Id, key, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(requestHash, result!.RequestHash);
        Assert.Equal(responseJson, result.ResponseJson);
    }

    [Fact]
    public async Task GetAsync_CuandoExistePeroOwnerEsDistinto_DebeRetornarNull()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, owner, _, transfer) = await SeedAsync(db);

        var store = new IdempotencyStore(db);

        await store.SaveSuccessAsync(owner.Id, transfer.Id, "k1", "H", "{}", CancellationToken.None);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await store.GetAsync(Guid.NewGuid(), "k1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_CuandoExistePeroKeyEsDistinto_DebeRetornarNull()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, owner, _, transfer) = await SeedAsync(db);

        var store = new IdempotencyStore(db);

        await store.SaveSuccessAsync(owner.Id, transfer.Id, "k1", "H", "{}", CancellationToken.None);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await store.GetAsync(owner.Id, "k2", CancellationToken.None);

        Assert.Null(result);
    }
}

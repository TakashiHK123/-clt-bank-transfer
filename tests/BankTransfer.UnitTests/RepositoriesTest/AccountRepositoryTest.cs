using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class AccountRepositoryTest
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

    private static async Task SeedUsuarioYCuentaAsync(BankTransferDbContext db, User user, params Account[] accounts)
    {
        db.Users.Add(user);
        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task GetByIdAsync_CuandoExisteCuenta_DebeRetornarCuenta()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, userId, "Caja", 100m, "PYG");

        await SeedUsuarioYCuentaAsync(db, user, acc);

        var repo = new AccountRepository(db);

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
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var repo = new AccountRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_CuandoHayVariasCuentas_DebeRetornarSoloLasDelUsuario()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var user1 = new User("user1", "hash", user1Id);
        var user2 = new User("user2", "hash", user2Id);

        var a1 = Account.Seed(Guid.NewGuid(), user1Id, "U1-A1", 10m, "PYG");
        var a2 = Account.Seed(Guid.NewGuid(), user1Id, "U1-A2", 20m, "PYG");
        var b1 = Account.Seed(Guid.NewGuid(), user2Id, "U2-B1", 30m, "PYG");

        db.Users.AddRange(user1, user2);
        db.Accounts.AddRange(a1, a2, b1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new AccountRepository(db);

        var list = await repo.GetByUserIdAsync(user1Id, CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.All(list, x => Assert.Equal(user1Id, x.UserId));
        Assert.Contains(list, x => x.Id == a1.Id);
        Assert.Contains(list, x => x.Id == a2.Id);
    }

    [Fact]
    public async Task GetByIdForUserAsync_CuandoCuentaPerteneceAlUsuario_DebeRetornarCuenta()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, userId, "Caja", 100m, "PYG");

        await SeedUsuarioYCuentaAsync(db, user, acc);

        var repo = new AccountRepository(db);

        var result = await repo.GetByIdForUserAsync(accountId, userId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(accountId, result!.Id);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByIdForUserAsync_CuandoCuentaNoPerteneceAlUsuario_DebeRetornarNull()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var user1 = new User("user1", "hash", user1Id);
        var user2 = new User("user2", "hash", user2Id);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, user1Id, "Caja", 100m, "PYG");

        db.Users.AddRange(user1, user2);
        db.Accounts.Add(acc);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new AccountRepository(db);

        var result = await repo.GetByIdForUserAsync(accountId, user2Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListByUserAsync_CuandoSeLlama_NoDebeTrackearEntidades()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var a1 = Account.Seed(Guid.NewGuid(), userId, "A1", 10m, "PYG");
        var a2 = Account.Seed(Guid.NewGuid(), userId, "A2", 20m, "PYG");

        await SeedUsuarioYCuentaAsync(db, user, a1, a2);

        var repo = new AccountRepository(db);
        
        db.ChangeTracker.Clear();

        var list = await repo.ListByUserAsync(userId, CancellationToken.None);

        Assert.Equal(2, list.Count);
        
        Assert.Empty(db.ChangeTracker.Entries<Account>());
    }

    [Fact]
    public async Task Update_CuandoSeActualizaEntidadDetached_DebePersistirCambios()
    {
        var (conn, db1) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db1;

        var userId = Guid.NewGuid();
        var user = new User("user1", "hash", userId);

        var accountId = Guid.NewGuid();
        var acc = Account.Seed(accountId, userId, "Caja", 100m, "PYG");

        await SeedUsuarioYCuentaAsync(db1, user, acc);

        var options2 = new DbContextOptionsBuilder<BankTransferDbContext>()
            .UseSqlite(conn)
            .Options;

        await using var db2 = new BankTransferDbContext(options2);
        var repo2 = new AccountRepository(db2);
        
        var detached = await db2.Accounts.AsNoTracking().FirstAsync(a => a.Id == accountId);
        
        var versionOriginal = detached.Version;
        
        detached.Credit(50m);
        
        repo2.Update(detached);
        db2.Entry(detached).Property(x => x.Version).OriginalValue = versionOriginal;

        await db2.SaveChangesAsync();

        var options3 = new DbContextOptionsBuilder<BankTransferDbContext>()
            .UseSqlite(conn)
            .Options;

        await using var db3 = new BankTransferDbContext(options3);

        var persisted = await db3.Accounts.FirstAsync(a => a.Id == accountId);

        Assert.Equal(150m, persisted.Balance);
        Assert.Equal(versionOriginal + 1, persisted.Version);
    }

}

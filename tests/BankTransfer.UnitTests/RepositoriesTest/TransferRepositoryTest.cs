using System.Reflection;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class TransferRepositoryTest
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

    private static async Task<(User U1, User U2, Account A1, Account A2, Account A3)> SeedUsuariosYCuentasAsync(BankTransferDbContext db)
    {
        var u1Id = Guid.NewGuid();
        var u2Id = Guid.NewGuid();

        var u1 = new User("user1", "hash", u1Id);
        var u2 = new User("user2", "hash", u2Id);

        var a1 = Account.Seed(Guid.NewGuid(), u1Id, "A1", 1000m, "PYG");
        var a2 = Account.Seed(Guid.NewGuid(), u2Id, "A2", 200m, "PYG");
        var a3 = Account.Seed(Guid.NewGuid(), u2Id, "A3", 300m, "PYG");

        db.Users.AddRange(u1, u2);
        db.Accounts.AddRange(a1, a2, a3);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return (u1, u2, a1, a2, a3);
    }

    private static void SetCreatedAt(Transfer t, DateTimeOffset value)
    {
        var field = typeof(Transfer).GetField("<CreatedAt>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
            throw new InvalidOperationException("No se encontró el backing field de CreatedAt. Cambió el modelo?");

        field.SetValue(t, value);
    }

    [Fact]
    public async Task AddAsync_CuandoSeAgregaTransferencia_DebePersistirEnBase()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, a1, a2, _) = await SeedUsuariosYCuentasAsync(db);

        var repo = new TransferRepository(db);

        var transfer = new Transfer(
            fromAccountId: a1.Id,
            toAccountId: a2.Id,
            amount: 10m,
            currency: "PYG",
            idempotencyKey: "k1");

        await repo.AddAsync(transfer, CancellationToken.None);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persisted = await db.Transfers.FirstOrDefaultAsync(x => x.Id == transfer.Id);

        Assert.NotNull(persisted);
        Assert.Equal(a1.Id, persisted!.FromAccountId);
        Assert.Equal(a2.Id, persisted.ToAccountId);
        Assert.Equal(10m, persisted.Amount);
        Assert.Equal("PYG", persisted.Currency);
        Assert.Equal("k1", persisted.IdempotencyKey);
    }

    [Fact]
    public async Task GetHistoryByAccountIdAsync_CuandoNoHayTransferencias_DebeRetornarListaVacia()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, a1, _, _) = await SeedUsuariosYCuentasAsync(db);

        var repo = new TransferRepository(db);

        var list = await repo.GetHistoryByAccountIdAsync(a1.Id, CancellationToken.None);

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetHistoryByAccountIdAsync_DebeRetornarSoloTransferenciasRelacionadasALaCuenta()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, a1, a2, a3) = await SeedUsuariosYCuentasAsync(db);
        
        var tOut = new Transfer(a1.Id, a2.Id, 10m, "PYG", "k-out");
        var tIn = new Transfer(a2.Id, a1.Id, 11m, "PYG", "k-in");
        
        var tOther = new Transfer(a2.Id, a3.Id, 12m, "PYG", "k-other");

        db.Transfers.AddRange(tOut, tIn, tOther);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new TransferRepository(db);

        var list = await repo.GetHistoryByAccountIdAsync(a1.Id, CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.Id == tOut.Id);
        Assert.Contains(list, x => x.Id == tIn.Id);
        Assert.DoesNotContain(list, x => x.Id == tOther.Id);
    }

    [Fact]
    public async Task GetHistoryByAccountIdAsync_DebeOrdenarPorCreatedAtDesc()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, a1, a2, _) = await SeedUsuariosYCuentasAsync(db);

        var t1 = new Transfer(a1.Id, a2.Id, 10m, "PYG", "k1");
        var t2 = new Transfer(a2.Id, a1.Id, 20m, "PYG", "k2");
        var t3 = new Transfer(a1.Id, a2.Id, 30m, "PYG", "k3");


        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        SetCreatedAt(t1, baseTime.AddMinutes(1)); // medio
        SetCreatedAt(t2, baseTime.AddMinutes(3)); // más nuevo
        SetCreatedAt(t3, baseTime.AddMinutes(2)); // segundo

        db.Transfers.AddRange(t1, t2, t3);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new TransferRepository(db);

        var list = await repo.GetHistoryByAccountIdAsync(a1.Id, CancellationToken.None);

        Assert.Equal(3, list.Count);
        
        Assert.Equal(t2.Id, list[0].Id);
        Assert.Equal(t3.Id, list[1].Id);
        Assert.Equal(t1.Id, list[2].Id);
    }

    [Fact]
    public async Task GetHistoryByAccountIdAsync_CuandoSeLlama_NoDebeTrackearEntidades()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var (_, _, a1, a2, _) = await SeedUsuariosYCuentasAsync(db);

        db.Transfers.Add(new Transfer(a1.Id, a2.Id, 10m, "PYG", "k1"));
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        Assert.Empty(db.ChangeTracker.Entries<Transfer>());

        var repo = new TransferRepository(db);

        var _hist = await repo.GetHistoryByAccountIdAsync(a1.Id, CancellationToken.None);
        
        Assert.Empty(db.ChangeTracker.Entries<Transfer>());
    }
}

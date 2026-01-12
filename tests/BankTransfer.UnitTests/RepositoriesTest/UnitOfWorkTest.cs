using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class UnitOfWorkTest
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

    [Fact]
    public async Task SaveChangesAsync_CuandoHayCambiosPendientes_DebePersistirYRetornarFilasAfectadas()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;
        
        var userId = Guid.NewGuid();
        db.Users.Add(new User("user1", "hash", userId));

        var uow = new UnitOfWork(db);
        
        var affected = await uow.SaveChangesAsync(CancellationToken.None);
        
        Assert.True(affected > 0);

        var persisted = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        Assert.NotNull(persisted);
        Assert.Equal("user1", persisted!.Username);
    }

    [Fact]
    public async Task SaveChangesAsync_CuandoNoHayCambios_DebeRetornarCero()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var uow = new UnitOfWork(db);

        var affected = await uow.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(0, affected);
    }
}
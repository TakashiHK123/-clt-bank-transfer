using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Persistence;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class UserRepositoryTest
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
    public async Task GetByUsernameAsync_CuandoNoExisteUsuario_DebeRetornarNull()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var repo = new UserRepository(db);

        var result = await repo.GetByUsernameAsync("no-existe", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_CuandoExisteUsuario_DebeRetornarUsuario()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var id = Guid.NewGuid();

        db.Users.Add(new User(username: "takashi", passwordHash: "hash", id: id));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new UserRepository(db);

        var result = await repo.GetByUsernameAsync("takashi", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("takashi", result.Username);
        Assert.Equal("hash", result.PasswordHash);
    }

    [Fact]
    public async Task GetByUsernameAsync_CuandoSePasaConEspaciosYMayusculas_DebeEncontrarPorTrimYLowercase()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var id = Guid.NewGuid();
        
        db.Users.Add(new User(username: "takashi", passwordHash: "hash", id: id));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new UserRepository(db);

        var result = await repo.GetByUsernameAsync("  TaKaShI  ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("takashi", result.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_CuandoExistePeroEnLaBaseEstaConMayusculas_NoDebeEncontrar()
    {
        var (conn, db) = await CrearDbAsync();
        await using var _ = conn;
        await using var __ = db;

        var id = Guid.NewGuid();
        
        db.Users.Add(new User(username: "Takashi", passwordHash: "hash", id: id));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new UserRepository(db);

        var result = await repo.GetByUsernameAsync("takashi", CancellationToken.None);

        Assert.Null(result);
    }
}

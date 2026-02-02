using System.Data;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Dapper;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class UserRepositoryTest
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

        return conn;
    }

    private static async Task SeedUserAsync(IDbConnection conn, User user)
    {
        await conn.ExecuteAsync(@"
            INSERT INTO Users (Id, Username, PasswordHash)
            VALUES (@Id, @Username, @PasswordHash)",
            new { user.Id, user.Username, user.PasswordHash });
    }

    [Fact]
    public async Task GetByUsernameAsync_CuandoNoExisteUsuario_DebeRetornarNull()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var repo = new UserRepository(conn);

        var result = await repo.GetByUsernameAsync("no-existe", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_CuandoExisteUsuario_DebeRetornarUsuario()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var id = Guid.NewGuid();
        var user = new User(username: "takashi", passwordHash: "hash", id: id);
        
        await SeedUserAsync(conn, user);

        var repo = new UserRepository(conn);

        var result = await repo.GetByUsernameAsync("takashi", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("takashi", result.Username);
        Assert.Equal("hash", result.PasswordHash);
    }

    [Fact]
    public async Task GetByUsernameAsync_CuandoSePasaConEspaciosYMayusculas_DebeEncontrarPorTrimYLowercase()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var id = Guid.NewGuid();
        var user = new User(username: "takashi", passwordHash: "hash", id: id);
        
        await SeedUserAsync(conn, user);

        var repo = new UserRepository(conn);

        var result = await repo.GetByUsernameAsync("  TaKaShI  ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("takashi", result.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_CuandoExistePeroEnLaBaseEstaConMayusculas_NoDebeEncontrar()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var id = Guid.NewGuid();
        var user = new User(username: "Takashi", passwordHash: "hash", id: id);
        
        await SeedUserAsync(conn, user);

        var repo = new UserRepository(conn);

        var result = await repo.GetByUsernameAsync("takashi", CancellationToken.None);

        Assert.Null(result);
    }
}

using System.Data;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class UnitOfWorkTest
{
    private static async Task<IDbConnection> CrearDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        return conn;
    }

    [Fact]
    public async Task SaveChangesAsync_CuandoNoHayTransaccion_DebeRetornarCero()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var uow = new UnitOfWork(conn);

        var result = await uow.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SaveChangesAsync_CuandoHayTransaccion_DebeCommitYRetornarUno()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var uow = new UnitOfWork(conn);
        
        uow.BeginTransaction();
        
        var result = await uow.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public void Dispose_CuandoSeLlama_DebeDisponerRecursos()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        var uow = new UnitOfWork(conn);

        // Should not throw
        uow.Dispose();
        
        Assert.True(true); // Test passes if no exception is thrown
    }
}

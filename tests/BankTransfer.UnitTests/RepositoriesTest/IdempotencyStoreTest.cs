using System.Data;
using BankTransfer.Application.Abstractions;
using BankTransfer.Domain.Entities;
using BankTransfer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Dapper;

namespace BankTransfer.UnitTests.RepositoriesTest;

public sealed class IdempotencyStoreTest
{
    private static async Task<IDbConnection> CrearDbAsync()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            CREATE TABLE IdempotencyRecords (
                Id TEXT PRIMARY KEY,
                AccountId TEXT NOT NULL,
                TransferId TEXT NOT NULL,
                Key TEXT NOT NULL,
                RequestHash TEXT NOT NULL,
                ResponseJson TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            )");

        return conn;
    }

    [Fact]
    public async Task GetAsync_CuandoNoExisteRegistro_DebeRetornarNull()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var store = new IdempotencyStore(conn);

        var result = await store.GetAsync(Guid.NewGuid(), "key1", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_CuandoExisteRegistro_DebeRetornarIdempotencyResult()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var accountId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var key = "test-key";
        var requestHash = "hash123";
        var responseJson = """{"id":"123","amount":100}""";

        // Insert test record
        await conn.ExecuteAsync(@"
            INSERT INTO IdempotencyRecords (Id, AccountId, TransferId, Key, RequestHash, ResponseJson, CreatedAt)
            VALUES (@Id, @AccountId, @TransferId, @Key, @RequestHash, @ResponseJson, @CreatedAt)",
            new {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                TransferId = transferId,
                Key = key,
                RequestHash = requestHash,
                ResponseJson = responseJson,
                CreatedAt = DateTime.UtcNow.ToString("O")
            });

        var store = new IdempotencyStore(conn);

        var result = await store.GetAsync(accountId, key, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(requestHash, result!.RequestHash);
        Assert.Equal(responseJson, result.ResponseJson);
    }

    [Fact]
    public async Task SaveSuccessAsync_CuandoSeGuardaRegistro_DebePoderRecuperarlo()
    {
        var conn = await CrearDbAsync();
        using var _ = conn;

        var accountId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var key = "save-test";
        var requestHash = "hash456";
        var responseJson = """{"success":true}""";

        var store = new IdempotencyStore(conn);

        await store.SaveSuccessAsync(accountId, transferId, key, requestHash, responseJson, CancellationToken.None);

        // Verify it was saved
        var result = await store.GetAsync(accountId, key, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(requestHash, result!.RequestHash);
        Assert.Equal(responseJson, result.ResponseJson);
    }
}

using BankTransfer.Domain.Entities;

namespace BankTransfer.UnitTests.Domain.Entities;

public sealed class IdempotencyRecordTests
{
    [Fact]
    public void Ctor_CuandoAccountIdEsVacio_DebeLanzarArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord(
                accountId: Guid.Empty,
                transferId: Guid.NewGuid(),
                key: "k",
                requestHash: "h",
                responseJson: "{}"));

        Assert.Equal("accountId", ex.ParamName);
    }

    [Fact]
    public void Ctor_CuandoTransferIdEsVacio_DebeLanzarArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord(
                accountId: Guid.NewGuid(),
                transferId: Guid.Empty,
                key: "k",
                requestHash: "h",
                responseJson: "{}"));

        Assert.Equal("transferId", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_CuandoKeyEsNuloOVacio_DebeLanzarArgumentException(string? key)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord(
                accountId: Guid.NewGuid(),
                transferId: Guid.NewGuid(),
                key: key!,
                requestHash: "h",
                responseJson: "{}"));

        Assert.Equal("key", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_CuandoRequestHashEsNuloOVacio_DebeLanzarArgumentException(string? requestHash)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord(
                accountId: Guid.NewGuid(),
                transferId: Guid.NewGuid(),
                key: "k",
                requestHash: requestHash!,
                responseJson: "{}"));

        Assert.Equal("requestHash", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_CuandoResponseJsonEsNuloOVacio_DebeLanzarArgumentException(string? responseJson)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord(
                accountId: Guid.NewGuid(),
                transferId: Guid.NewGuid(),
                key: "k",
                requestHash: "h",
                responseJson: responseJson!));

        Assert.Equal("responseJson", ex.ParamName);
    }

    [Fact]
    public void Ctor_CuandoEsValido_DebeSetearPropiedades_RecortarInputs_YSetearCreatedAtUtc()
    {
        var accountId = Guid.NewGuid();
        var transferId = Guid.NewGuid();

        var before = DateTime.UtcNow;

        var rec = new IdempotencyRecord(
            accountId: accountId,
            transferId: transferId,
            key: "  key-123  ",
            requestHash: "  hash-abc  ",
            responseJson: "{\"ok\":true}");

        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, rec.Id);

        Assert.Equal(accountId, rec.AccountId);
        Assert.Equal(transferId, rec.TransferId);

        Assert.Equal("key-123", rec.Key);
        Assert.Equal("hash-abc", rec.RequestHash);
        Assert.Equal("{\"ok\":true}", rec.ResponseJson);

        Assert.True(rec.CreatedAtUtc >= before && rec.CreatedAtUtc <= after,
            $"CreatedAtUtc fuera de rango. before={before:o}, created={rec.CreatedAtUtc:o}, after={after:o}");
    }
}

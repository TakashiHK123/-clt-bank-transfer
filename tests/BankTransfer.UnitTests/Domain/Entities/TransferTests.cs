using System;
using BankTransfer.Domain.Entities;
using BankTransfer.Domain.Exceptions;
using Xunit;

namespace BankTransfer.UnitTests.Domain.Entities;

public sealed class TransferTests
{
    [Fact]
    public void Ctor_CuandoFromAccountIdEsVacio_DebeLanzarArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transfer(
                fromAccountId: Guid.Empty,
                toAccountId: Guid.NewGuid(),
                amount: 10m,
                currency: "PYG",
                idempotencyKey: "k"));

        Assert.Contains("FromAccountId is required", ex.Message);
    }

    [Fact]
    public void Ctor_CuandoToAccountIdEsVacio_DebeLanzarArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transfer(
                fromAccountId: Guid.NewGuid(),
                toAccountId: Guid.Empty,
                amount: 10m,
                currency: "PYG",
                idempotencyKey: "k"));

        Assert.Contains("ToAccountId is required", ex.Message);
    }

    [Fact]
    public void Ctor_CuandoFromYToSonIguales_DebeLanzarSameAccountTransferException()
    {
        var id = Guid.NewGuid();

        var ex = Assert.Throws<SameAccountTransferException>(() =>
            new Transfer(
                fromAccountId: id,
                toAccountId: id,
                amount: 10m,
                currency: "PYG",
                idempotencyKey: "k"));

        Assert.Contains("FromAccountId and ToAccountId cannot be the same.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ctor_CuandoAmountEsCeroONegativo_DebeLanzarArgumentOutOfRangeException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Transfer(
                fromAccountId: Guid.NewGuid(),
                toAccountId: Guid.NewGuid(),
                amount: amount,
                currency: "PYG",
                idempotencyKey: "k"));

        Assert.Equal("amount", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_CuandoCurrencyEsNuloOVacio_DebeLanzarArgumentException(string? currency)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transfer(
                fromAccountId: Guid.NewGuid(),
                toAccountId: Guid.NewGuid(),
                amount: 10m,
                currency: currency!,
                idempotencyKey: "k"));

        Assert.Equal("currency", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_CuandoIdempotencyKeyEsNuloOVacio_DebeLanzarArgumentException(string? key)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Transfer(
                fromAccountId: Guid.NewGuid(),
                toAccountId: Guid.NewGuid(),
                amount: 10m,
                currency: "PYG",
                idempotencyKey: key!));

        Assert.Contains("IdempotencyKey is required", ex.Message);
    }

    [Fact]
    public void Ctor_CuandoEsValido_DebeSetearPropiedades_RecortarYMayusculizar_YSetearCreatedAtUtc()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();

        var before = DateTimeOffset.UtcNow;

        var t = new Transfer(
            fromAccountId: fromId,
            toAccountId: toId,
            amount: 123.45m,
            currency: " pyg ",
            idempotencyKey: "  idem-1  ");

        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, t.Id);

        Assert.Equal(fromId, t.FromAccountId);
        Assert.Equal(toId, t.ToAccountId);
        Assert.Equal(123.45m, t.Amount);

        Assert.Equal("PYG", t.Currency);
        Assert.Equal("idem-1", t.IdempotencyKey);

        Assert.True(t.CreatedAt >= before && t.CreatedAt <= after,
            $"CreatedAt fuera de rango. before={before:o}, created={t.CreatedAt:o}, after={after:o}");

        Assert.Null(t.IdempotencyRecord);
    }
}

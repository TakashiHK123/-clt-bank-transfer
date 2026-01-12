using System;
using BankTransfer.Domain.Entities;
using Xunit;

namespace BankTransfer.UnitTests.Domain.Entities;

public sealed class UserTests
{
    [Fact]
    public void Ctor_CuandoIdEsVacio_DebeLanzarArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new User(username: "user", passwordHash: "hash", id: Guid.Empty));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_CuandoUsernameEsNuloOVacio_DebeLanzarArgumentException(string? username)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new User(username: username!, passwordHash: "hash", id: Guid.NewGuid()));

        Assert.Equal("username", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_CuandoPasswordHashEsNuloOVacio_DebeLanzarArgumentException(string? passwordHash)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new User(username: "user", passwordHash: passwordHash!, id: Guid.NewGuid()));

        Assert.Equal("passwordHash", ex.ParamName);
    }

    [Fact]
    public void Ctor_CuandoEsValido_DebeSetearPropiedades_RecortarUsername_YInicializarAccounts()
    {
        var id = Guid.NewGuid();

        var u = new User(username: "  takashi  ", passwordHash: "hash-123", id: id);

        Assert.Equal(id, u.Id);
        Assert.Equal("takashi", u.Username);
        Assert.Equal("hash-123", u.PasswordHash);

        Assert.NotNull(u.Accounts);
        Assert.Empty(u.Accounts);
    }
}
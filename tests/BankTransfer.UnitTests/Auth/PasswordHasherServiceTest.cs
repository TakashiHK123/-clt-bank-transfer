using BankTransfer.Infrastructure.Auth;

namespace BankTransfer.UnitTests.Auth;

public sealed class PasswordHasherServiceTests
{
    [Fact]
    public void Hash_CuandoSeHasheaUnaPassword_DebeRetornarUnHashNoVacio_YDistintoALaPassword()
    {
        var svc = new PasswordHasherService();

        var password = "MiPassword-123!";

        var hash = svc.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void Verify_CuandoLaPasswordEsCorrecta_DebeRetornarTrue()
    {
        var svc = new PasswordHasherService();

        var password = "MiPassword-123!";
        var hash = svc.Hash(password);

        var ok = svc.Verify(password, hash);

        Assert.True(ok);
    }

    [Fact]
    public void Verify_CuandoLaPasswordEsIncorrecta_DebeRetornarFalse()
    {
        var svc = new PasswordHasherService();

        var password = "MiPassword-123!";
        var hash = svc.Hash(password);

        var ok = svc.Verify("otra-password", hash);

        Assert.False(ok);
    }

    [Fact]
    public void Hash_CuandoSeHasheaLaMismaPassword_DosVeces_DebeGenerarHashesDistintos()
    {
        var svc = new PasswordHasherService();

        var password = "MiPassword-123!";

        var h1 = svc.Hash(password);
        var h2 = svc.Hash(password);

        Assert.NotEqual(h1, h2);

        Assert.True(svc.Verify(password, h1));
        Assert.True(svc.Verify(password, h2));
    }

    [Fact]
    public void Verify_CuandoElHashEsInvalido_DebeRetornarFalse()
    {
        var svc = new PasswordHasherService();

        var ok = svc.Verify("MiPassword123", "hashInvalido");

        Assert.False(ok);
    }
}
using BankTransfer.Application.Abstractions;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.Abstractions.Services;
using BankTransfer.Application.DTOs;
using BankTransfer.Application.Services;
using BankTransfer.Domain.Entities;
using Moq;

namespace BankTransfer.UnitTests.ServicesTest;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_CuandoUsuarioNoExiste_DebeLanzarUnauthorizedAccessException()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var hasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var tokens = new Mock<ITokenService>(MockBehavior.Strict);

        var ct = new CancellationTokenSource().Token;

        var req = new LoginRequestDto("no-existe", "123");

        users
            .Setup(x => x.GetByUsernameAsync(req.Username, ct))
            .ReturnsAsync((User?)null);

        var svc = new AuthService(users.Object, hasher.Object, tokens.Object);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.LoginAsync(req, ct));
        Assert.Equal("Invalid credentials", ex.Message);

        users.VerifyAll();
        hasher.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_CuandoPasswordEsIncorrecto_DebeLanzarUnauthorizedAccessException()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var hasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var tokens = new Mock<ITokenService>(MockBehavior.Strict);

        var ct = new CancellationTokenSource().Token;

        var req = new LoginRequestDto("takashi", "wrong-pass");
        var user = new User(username: "takashi", passwordHash: "hash-ok", id: Guid.NewGuid());

        users
            .Setup(x => x.GetByUsernameAsync(req.Username, ct))
            .ReturnsAsync(user);

        hasher
            .Setup(x => x.Verify(req.Password, user.PasswordHash))
            .Returns(false);

        var svc = new AuthService(users.Object, hasher.Object, tokens.Object);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.LoginAsync(req, ct));
        Assert.Equal("Invalid credentials", ex.Message);

        users.VerifyAll();
        hasher.VerifyAll();
        tokens.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_CuandoCredencialesSonValidas_DebeRetornarToken()
    {
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var hasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var tokens = new Mock<ITokenService>(MockBehavior.Strict);

        var ct = new CancellationTokenSource().Token;

        var req = new LoginRequestDto("takashi", "123");
        var userId = Guid.NewGuid();
        var user = new User(username: "takashi", passwordHash: "hash-ok", id: userId);

        users
            .Setup(x => x.GetByUsernameAsync(req.Username, ct))
            .ReturnsAsync(user);

        hasher
            .Setup(x => x.Verify(req.Password, user.PasswordHash))
            .Returns(true);

        tokens
            .Setup(x => x.CreateToken(user.Id, user.Username))
            .Returns("token-jwt");

        var svc = new AuthService(users.Object, hasher.Object, tokens.Object);

        var result = await svc.LoginAsync(req, ct);

        Assert.NotNull(result);
        Assert.Equal("token-jwt", result.AccessToken);

        users.VerifyAll();
        hasher.VerifyAll();
        tokens.VerifyAll();
    }
}

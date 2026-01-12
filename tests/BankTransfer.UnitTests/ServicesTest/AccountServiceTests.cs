using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.Services;
using BankTransfer.Domain.Entities;
using Moq;

namespace BankTransfer.UnitTests.ServicesTest;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task GetByIdForUserAsync_CuandoNoExisteCuenta_DebeRetornarNull()
    {
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        var ct = new CancellationTokenSource().Token;

        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        accounts
            .Setup(x => x.GetByIdForUserAsync(accountId, userId, ct))
            .ReturnsAsync((Account?)null);

        var svc = new AccountService(accounts.Object);

        var result = await svc.GetByIdForUserAsync(accountId, userId, ct);

        Assert.Null(result);
        accounts.VerifyAll();
    }

    [Fact]
    public async Task GetByIdForUserAsync_CuandoExisteCuenta_DebeMapearYRetornarDto()
    {
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        var ct = new CancellationTokenSource().Token;

        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var acc = Account.Seed(
            id: accountId,
            userId: userId,
            name: "Caja",
            initialBalance: 1500m,
            currency: "PYG");

        accounts
            .Setup(x => x.GetByIdForUserAsync(accountId, userId, ct))
            .ReturnsAsync(acc);

        var svc = new AccountService(accounts.Object);

        var result = await svc.GetByIdForUserAsync(accountId, userId, ct);

        Assert.NotNull(result);
        Assert.Equal("Caja", result!.Name);
        Assert.Equal(1500m, result.Amount);
        Assert.Equal("PYG", result.Currency);

        accounts.VerifyAll();
    }

    [Fact]
    public async Task GetByUserIdAsync_CuandoRepositorioRetornaListaVacia_DebeRetornarListaVacia()
    {
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        var ct = new CancellationTokenSource().Token;

        var userId = Guid.NewGuid();

        accounts
            .Setup(x => x.GetByUserIdAsync(userId, ct))
            .ReturnsAsync(new List<Account>());

        var svc = new AccountService(accounts.Object);

        var result = await svc.GetByUserIdAsync(userId, ct);

        Assert.NotNull(result);
        Assert.Empty(result);

        accounts.VerifyAll();
    }

    [Fact]
    public async Task GetByUserIdAsync_CuandoRepositorioRetornaCuentas_DebeMapearYRetornarDtosConId()
    {
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        var ct = new CancellationTokenSource().Token;

        var userId = Guid.NewGuid();

        var a1 = Account.Seed(Guid.NewGuid(), userId, "A1", 10m, "PYG");
        var a2 = Account.Seed(Guid.NewGuid(), userId, "A2", 20m, "usd");

        accounts
            .Setup(x => x.GetByUserIdAsync(userId, ct))
            .ReturnsAsync(new List<Account> { a1, a2 });

        var svc = new AccountService(accounts.Object);

        var result = await svc.GetByUserIdAsync(userId, ct);

        Assert.Equal(2, result.Count);

        var r1 = result.Single(x => x.Id == a1.Id);
        Assert.Equal("A1", r1.Name);
        Assert.Equal(10m, r1.Amount);
        Assert.Equal("PYG", r1.Currency);

        var r2 = result.Single(x => x.Id == a2.Id);
        Assert.Equal("A2", r2.Name);
        Assert.Equal(20m, r2.Amount);
        Assert.Equal("USD", r2.Currency); 

        accounts.VerifyAll();
    }
}

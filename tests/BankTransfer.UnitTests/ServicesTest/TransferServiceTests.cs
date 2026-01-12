using BankTransfer.Application.Abstractions;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.DTOs;
using BankTransfer.Application.Services;
using BankTransfer.Domain.Entities;
using Moq;

namespace BankTransfer.UnitTests.ServicesTest;

public sealed class TransferServiceTests
{
    [Fact]
    public async Task CreateAsync_CuandoSeLlama_DebeUsarIdempotencyKeyComoString()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "FROM", 1000m, "PYG");
        var to = new Account(Guid.NewGuid(), "TO", 100m, "PYG");

        var accounts = new Mock<IAccountRepository>();


        accounts
            .Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);

        accounts
            .Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(to);

        accounts.Setup(x => x.Update(from));
        accounts.Setup(x => x.Update(to));

        var transfers = new Mock<ITransferRepository>();
        transfers
            .Setup(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var capturedKey = (string?)null;

        var idem = new Mock<IIdempotencyStore>();
        idem
            .Setup(x => x.GetAsync(from.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, CancellationToken>((_, key, _) => capturedKey = key)
            .ReturnsAsync((IdempotencyResult?)null);

        idem
            .Setup(x => x.SaveSuccessAsync(
                from.Id,
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var transferFunds = new TransferFundsService(accounts.Object, transfers.Object, idem.Object, uow.Object);
        var svc = new TransferService(accounts.Object, transfers.Object, transferFunds);

        var request = new TransferRequestDto(from.Id, to.Id, 10m);
        var idemGuid = Guid.NewGuid();
        var ct = new CancellationTokenSource().Token;

        var result = await svc.CreateAsync(userId, request, idemGuid, ct);

        Assert.NotNull(result);
        Assert.Equal(idemGuid.ToString(), capturedKey);

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryByAccountAsync_CuandoLaCuentaNoEsDelUsuario_DebeRetornarNull()
    {
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        var transfers = new Mock<ITransferRepository>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var ct = new CancellationTokenSource().Token;

        accounts
            .Setup(x => x.GetByIdForUserAsync(accountId, userId, ct))
            .ReturnsAsync((Account?)null);

        var dummyFunds = new TransferFundsService(
            accounts.Object,
            transfers.Object,
            Mock.Of<IIdempotencyStore>(),
            Mock.Of<IUnitOfWork>());

        var svc = new TransferService(accounts.Object, transfers.Object, dummyFunds);

        var result = await svc.GetHistoryByAccountAsync(userId, accountId, ct);

        Assert.Null(result);

        accounts.VerifyAll();
        transfers.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetHistoryByAccountAsync_CuandoLaCuentaEsDelUsuario_DebeMapearHistorialConDireccionIN_OUT()
    {
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        var transfers = new Mock<ITransferRepository>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var ct = new CancellationTokenSource().Token;

        var acc = Account.Seed(accountId, userId, "ACC", 0m, "PYG");

        accounts
            .Setup(x => x.GetByIdForUserAsync(accountId, userId, ct))
            .ReturnsAsync(acc);

        var tOut = new Transfer(
            fromAccountId: accountId,
            toAccountId: Guid.NewGuid(),
            amount: 50m,
            currency: "PYG",
            idempotencyKey: "k1");

        var tIn = new Transfer(
            fromAccountId: Guid.NewGuid(),
            toAccountId: accountId,
            amount: 25m,
            currency: "PYG",
            idempotencyKey: "k2");

        transfers
            .Setup(x => x.GetHistoryByAccountIdAsync(accountId, ct))
            .ReturnsAsync(new List<Transfer> { tOut, tIn });

        var dummyFunds = new TransferFundsService(
            accounts.Object,
            transfers.Object,
            Mock.Of<IIdempotencyStore>(),
            Mock.Of<IUnitOfWork>());

        var svc = new TransferService(accounts.Object, transfers.Object, dummyFunds);

        var result = await svc.GetHistoryByAccountAsync(userId, accountId, ct);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);

        var outItem = result.Single(x => x.Id == tOut.Id);
        Assert.Equal("OUT", outItem.Direction);

        var inItem = result.Single(x => x.Id == tIn.Id);
        Assert.Equal("IN", inItem.Direction);

        accounts.VerifyAll();
        transfers.VerifyAll();
    }
}

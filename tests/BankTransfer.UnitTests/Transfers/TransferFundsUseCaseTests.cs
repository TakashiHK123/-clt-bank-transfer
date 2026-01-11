using BankTransfer.Application.Abstractions;
using BankTransfer.Application.DTOs;
using BankTransfer.Application.UseCases;
using BankTransfer.Domain.Entities;
using BankTransfer.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BankTransfer.UnitTests.Transfers;

public sealed class TransferFundsUseCaseTests
{
    [Fact]
    public async Task Test_CuandoHayFondosSuficientes_DebeCrearTransferencia()
    {
        // user dueño de la cuenta origen
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 1000m, "PYG");
        var to = new Account(Guid.NewGuid(), "B", 100m, "PYG"); // puede ser de otro user

        var accounts = new Mock<IAccountRepository>();

        // origen: debe ser del usuario (ownership)
        accounts
            .Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);

        // destino: lookup normal
        accounts
            .Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(to);

        var transfers = new Mock<ITransferRepository>();

        var idem = new Mock<IIdempotencyStore>();
        idem
            .Setup(x => x.GetAsync(userId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyResult?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new TransferFundsUseCase(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var res = await useCase.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, to.Id, 200m),
            "k1",
            CancellationToken.None);

        res.Amount.Should().Be(200m);
        from.Balance.Should().Be(800m);
        to.Balance.Should().Be(300m);

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);

        idem.Verify(x => x.SaveSuccessAsync(
                userId, "k1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_De_Fondos_Insuficientes()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 100m, "PYG");
        var to = new Account(Guid.NewGuid(), "B", 100m, "PYG");

        var accounts = new Mock<IAccountRepository>();

        accounts
            .Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);

        accounts
            .Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(to);

        var idem = new Mock<IIdempotencyStore>();
        idem
            .Setup(x => x.GetAsync(userId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyResult?)null);

        var useCase = new TransferFundsUseCase(
            accounts.Object,
            Mock.Of<ITransferRepository>(),
            idem.Object,
            Mock.Of<IUnitOfWork>());

        var act = async () => await useCase.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, to.Id, 200m),
            "k1",
            CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task Test_Misma_Idempotency_No_Duplica_Devuelve_Lo_Mismo_Que_La_Anterior()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 1000m, "PYG");
        var to = new Account(Guid.NewGuid(), "B", 100m, "PYG");

        var accounts = new Mock<IAccountRepository>();

        accounts
            .Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);

        accounts
            .Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(to);

        var transfers = new Mock<ITransferRepository>();

        var idem = new Mock<IIdempotencyStore>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new TransferFundsUseCase(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var key = "k-retry";
        var req = new TransferRequestDto(from.Id, to.Id, 200m);

        string? savedHash = null;
        string? savedResponseJson = null;

        idem
            .Setup(x => x.SaveSuccessAsync(userId, key, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, string, CancellationToken>((_, __, h, json, ___) =>
            {
                savedHash = h;
                savedResponseJson = json;
            })
            .Returns(Task.CompletedTask);

        var getCount = 0;
        idem
            .Setup(x => x.GetAsync(userId, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                getCount++;
                return getCount == 1
                    ? null
                    : new IdempotencyResult(savedHash!, savedResponseJson!);
            });

        var first = await useCase.ExecuteAsync(userId, req, key, CancellationToken.None);

        savedHash.Should().NotBeNull();
        savedResponseJson.Should().NotBeNull();

        var second = await useCase.ExecuteAsync(userId, req, key, CancellationToken.None);

        second.Should().BeEquivalentTo(first);

        // no duplica: solo una ejecución “real”
        from.Balance.Should().Be(800m);
        to.Balance.Should().Be(300m);

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);

        idem.Verify(x => x.SaveSuccessAsync(
                userId, key, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_Monedas_Distintas_Debe_Fallar()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 1000m, "USD");
        var to = new Account(Guid.NewGuid(), "B", 100m, "PYG");

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);
        accounts.Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(to);

        var idem = new Mock<IIdempotencyStore>();
        idem.Setup(x => x.GetAsync(userId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyResult?)null);

        var useCase = new TransferFundsUseCase(
            accounts.Object,
            Mock.Of<ITransferRepository>(),
            idem.Object,
            Mock.Of<IUnitOfWork>());

        var act = async () => await useCase.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, to.Id, 10m),
            "k1",
            CancellationToken.None);

        await act.Should().ThrowAsync<CurrencyMismatchException>();
    }

}

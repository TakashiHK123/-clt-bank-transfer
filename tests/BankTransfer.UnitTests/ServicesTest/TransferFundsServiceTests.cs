using System.Security.Cryptography;
using System.Text;
using BankTransfer.Application.Abstractions;
using BankTransfer.Application.Abstractions.Repositories;
using BankTransfer.Application.DTOs;
using BankTransfer.Application.Services;
using BankTransfer.Domain.Entities;
using BankTransfer.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BankTransfer.UnitTests.ServicesTest;

public sealed class TransferFundsServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CuandoHayFondosSuficientes_DebeCrearTransferencia()
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

        var ownerId = from.Id;

        var idem = new Mock<IIdempotencyStore>();
        idem
            .Setup(x => x.GetAsync(ownerId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyResult?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = new TransferFundsService(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var res = await service.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, to.Id, 200m),
            "k1",
            CancellationToken.None);

        res.Amount.Should().Be(200m);
        from.Balance.Should().Be(800m);
        to.Balance.Should().Be(300m);

        // Se persiste la transferencia
        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);

        // Se actualizan cuentas
        accounts.Verify(x => x.Update(from), Times.Once);
        accounts.Verify(x => x.Update(to), Times.Once);

        // Se guarda idempotencia y commit
        idem.Verify(x => x.SaveSuccessAsync(
                ownerId,
                It.IsAny<Guid>(),
                "k1",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuandoFondosSonInsuficientes_DebeLanzarInsufficientFundsException()
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

        var ownerId = from.Id;

        var idem = new Mock<IIdempotencyStore>();
        idem
            .Setup(x => x.GetAsync(ownerId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyResult?)null);

        var service = new TransferFundsService(
            accounts.Object,
            Mock.Of<ITransferRepository>(),
            idem.Object,
            Mock.Of<IUnitOfWork>());

        var act = async () => await service.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, to.Id, 200m),
            "k1",
            CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task ExecuteAsync_CuandoSeRepiteIdempotencyKey_DebeRetornarMismaRespuesta_YNoDuplicar()
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

        var ownerId = from.Id;

        var idem = new Mock<IIdempotencyStore>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = new TransferFundsService(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var key = "k-retry";
        var req = new TransferRequestDto(from.Id, to.Id, 200m);

        string? savedHash = null;
        string? savedResponseJson = null;

        idem
            .Setup(x => x.SaveSuccessAsync(
                ownerId,
                It.IsAny<Guid>(),
                key,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback((Guid _owner, Guid _transferId, string _key, string h, string json, CancellationToken _) =>
            {
                savedHash = h;
                savedResponseJson = json;
            })
            .Returns(Task.CompletedTask);

        var getCount = 0;
        idem
            .Setup(x => x.GetAsync(ownerId, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                getCount++;
                return getCount == 1
                    ? null
                    : new IdempotencyResult(savedHash!, savedResponseJson!);
            });

        var first = await service.ExecuteAsync(userId, req, key, CancellationToken.None);

        savedHash.Should().NotBeNull();
        savedResponseJson.Should().NotBeNull();

        var second = await service.ExecuteAsync(userId, req, key, CancellationToken.None);

        second.Should().BeEquivalentTo(first);

        // No duplica ejecución real
        from.Balance.Should().Be(800m);
        to.Balance.Should().Be(300m);

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);

        idem.Verify(x => x.SaveSuccessAsync(
                ownerId,
                It.IsAny<Guid>(),
                key,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CuandoMonedasSonDistintas_DebeLanzarCurrencyMismatchException()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 1000m, "USD");
        var to = new Account(Guid.NewGuid(), "B", 100m, "PYG");

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);
        accounts.Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(to);

        var ownerId = from.Id;

        var idem = new Mock<IIdempotencyStore>();
        idem.Setup(x => x.GetAsync(ownerId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyResult?)null);

        var transfers = new Mock<ITransferRepository>();
        var uow = new Mock<IUnitOfWork>();

        var service = new TransferFundsService(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var act = async () => await service.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, to.Id, 10m),
            "k1",
            CancellationToken.None);

        await act.Should().ThrowAsync<CurrencyMismatchException>();

        // En mismatch no se persiste nada
        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Never);
        idem.Verify(x => x.SaveSuccessAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================
    // TESTS QUE FALTABAN
    // =========================

    [Fact]
    public async Task ExecuteAsync_CuandoCuentaOrigenNoExisteParaUsuario_DebeLanzarAccountNotFoundException()
    {
        var userId = Guid.NewGuid();

        var accounts = new Mock<IAccountRepository>();
        accounts
            .Setup(x => x.GetByIdForUserAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var service = new TransferFundsService(
            accounts.Object,
            Mock.Of<ITransferRepository>(),
            Mock.Of<IIdempotencyStore>(),
            Mock.Of<IUnitOfWork>());

        var req = new TransferRequestDto(Guid.NewGuid(), Guid.NewGuid(), 10m);

        var act = async () => await service.ExecuteAsync(userId, req, "k1", CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_CuandoCuentaDestinoNoExiste_DebeLanzarAccountNotFoundException()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 1000m, "PYG");
        var toId = Guid.NewGuid();

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);

        var ownerId = from.Id;

        var idem = new Mock<IIdempotencyStore>();
        idem.Setup(x => x.GetAsync(ownerId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyResult?)null);

        accounts.Setup(x => x.GetByIdAsync(toId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var transfers = new Mock<ITransferRepository>();
        var uow = new Mock<IUnitOfWork>();

        var service = new TransferFundsService(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var act = async () => await service.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, toId, 10m),
            "k1",
            CancellationToken.None);

        await act.Should().ThrowAsync<AccountNotFoundException>();

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CuandoHayCacheConHashDistinto_DebeLanzarIdempotencyConflictException_YNoCambiarSaldos()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 1000m, "PYG");
        var to = new Account(Guid.NewGuid(), "B", 100m, "PYG");

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);

        var ownerId = from.Id;

        var idem = new Mock<IIdempotencyStore>();
        idem.Setup(x => x.GetAsync(ownerId, "k1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyResult(
                RequestHash: "HASH-DIFERENTE",
                ResponseJson: "{}"));

        var transfers = new Mock<ITransferRepository>();
        var uow = new Mock<IUnitOfWork>();

        var service = new TransferFundsService(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var act = async () => await service.ExecuteAsync(
            userId,
            new TransferRequestDto(from.Id, to.Id, 200m),
            "k1",
            CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyConflictException>();

        // No toca estado ni persiste nada
        from.Balance.Should().Be(1000m);
        to.Balance.Should().Be(100m);

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Never);
        idem.Verify(x => x.SaveSuccessAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        // Importante: en conflicto ni siquiera debería buscar cuenta destino
        accounts.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CuandoHayCacheConJsonNull_DebeLanzarInvalidOperationException()
    {
        var userId = Guid.NewGuid();

        var from = new Account(userId, "A", 1000m, "PYG");
        var to = new Account(Guid.NewGuid(), "B", 100m, "PYG");

        var req = new TransferRequestDto(from.Id, to.Id, 200m);
        var key = "k1";
        var ownerId = from.Id;

        var hashCorrecto = CalcularHash($"{req.FromAccountId}|{req.ToAccountId}|{req.Amount}");

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdForUserAsync(from.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(from);

        var idem = new Mock<IIdempotencyStore>();
        idem.Setup(x => x.GetAsync(ownerId, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyResult(hashCorrecto, "null")); // Deserialize => null

        var transfers = new Mock<ITransferRepository>();
        var uow = new Mock<IUnitOfWork>();

        var service = new TransferFundsService(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var act = async () => await service.ExecuteAsync(userId, req, key, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("Stored idempotency response is invalid");

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static string CalcularHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

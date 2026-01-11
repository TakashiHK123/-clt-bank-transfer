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
    public async Task ExecuteAsync_WhenEnoughFunds_ShouldCreateTransfer()
    {
        var from = new Account("A", 1000m);
        var to = new Account("B", 100m);

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdAsync(from.Id, It.IsAny<CancellationToken>())).ReturnsAsync(from);
        accounts.Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>())).ReturnsAsync(to);

        var transfers = new Mock<ITransferRepository>();
        var idem = new Mock<IIdempotencyStore>();
        idem.Setup(x => x.GetAsync("k1", It.IsAny<CancellationToken>())).ReturnsAsync((IdempotencyResult?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new TransferFundsUseCase(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var res = await useCase.ExecuteAsync(new TransferRequestDto(from.Id, to.Id, 200m), "k1", CancellationToken.None);

        res.Amount.Should().Be(200m);
        from.Balance.Should().Be(800m);
        to.Balance.Should().Be(300m);
        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);
        idem.Verify(x => x.SaveSuccessAsync("k1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInsufficientFunds_ShouldThrow()
    {
        var from = new Account("A", 100m);
        var to = new Account("B", 100m);

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdAsync(from.Id, It.IsAny<CancellationToken>())).ReturnsAsync(from);
        accounts.Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>())).ReturnsAsync(to);

        var useCase = new TransferFundsUseCase(
            accounts.Object,
            Mock.Of<ITransferRepository>(),
            Mock.Of<IIdempotencyStore>(),
            Mock.Of<IUnitOfWork>());

        var act = async () => await useCase.ExecuteAsync(new TransferRequestDto(from.Id, to.Id, 200m), "k1", CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenRetriedWithSameIdempotencyKey_ShouldReturnSameResponse_AndNotDuplicate()
    {
        var from = new Account("A", 1000m);
        var to = new Account("B", 100m);

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetByIdAsync(from.Id, It.IsAny<CancellationToken>())).ReturnsAsync(from);
        accounts.Setup(x => x.GetByIdAsync(to.Id, It.IsAny<CancellationToken>())).ReturnsAsync(to);

        var transfers = new Mock<ITransferRepository>();

        var idem = new Mock<IIdempotencyStore>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new TransferFundsUseCase(accounts.Object, transfers.Object, idem.Object, uow.Object);

        var key = "k-retry";
        var req = new TransferRequestDto(from.Id, to.Id, 200m);

        string? savedHash = null;
        string? savedResponseJson = null;

        idem.Setup(x => x.SaveSuccessAsync(key, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, h, json, __) =>
            {
                savedHash = h;
                savedResponseJson = json;
            })
            .Returns(Task.CompletedTask);

        var getCount = 0;
        idem.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                getCount++;
                return getCount == 1
                    ? null
                    : new IdempotencyResult(savedHash!, savedResponseJson!);
            });

        var first = await useCase.ExecuteAsync(req, key, CancellationToken.None);

        savedHash.Should().NotBeNull();
        savedResponseJson.Should().NotBeNull();

        var second = await useCase.ExecuteAsync(req, key, CancellationToken.None);

        second.Should().BeEquivalentTo(first);

        from.Balance.Should().Be(800m);
        to.Balance.Should().Be(300m);

        transfers.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);
        idem.Verify(x => x.SaveSuccessAsync(key, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

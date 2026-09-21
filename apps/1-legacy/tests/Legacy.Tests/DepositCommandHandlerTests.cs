using Legacy.Application.Commands;
using Legacy.Domain;
using Legacy.Domain.Entities;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Providers;
using Legacy.Domain.Repositories;
using Moq;

namespace Legacy.Tests;

public class DepositCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IPaymentTransactionRepository> _transactions = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPaymentProviderFactory> _factory = new();
    private readonly Mock<IPaymentProvider> _provider = new();
    private readonly DepositCommandHandler _handler;

    public DepositCommandHandlerTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Customers).Returns(_customers.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.Transactions).Returns(_transactions.Object);
        _provider.Setup(provider => provider.Name).Returns("stripe");
        _handler = new DepositCommandHandler(_unitOfWork.Object, _factory.Object);
    }

    [Fact]
    public async Task When_deposit_succeeds_balance_is_updated_and_transaction_is_written()
    {
        var customer = Alice();
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _factory.Setup(factory => factory.Create("stripe")).Returns(_provider.Object);
        _provider.Setup(provider => provider.DepositAsync(SeedData.AliceId, 10.00m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderResult { IsApproved = true, ExternalReference = "stripe-ref" });

        var result = await _handler.Handle(new DepositCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 10.00m,
            Provider = "stripe"
        }, CancellationToken.None);

        Assert.Equal(110.00m, result.Balance);
        Assert.Equal(SeedData.AliceId, result.CustomerId);
        _transactions.Verify(repository => repository.AddAsync(It.Is<PaymentTransaction>(transaction =>
            transaction.CustomerId == SeedData.AliceId &&
            transaction.Amount == 10.00m &&
            transaction.Provider == "stripe"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task When_customer_is_unknown_deposit_throws_AccountNotFound()
    {
        _customers.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<AccountNotFoundException>(() => _handler.Handle(new DepositCommand
        {
            CustomerId = Guid.NewGuid(),
            Amount = 10.00m,
            Provider = "stripe"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task When_amount_is_invalid_deposit_throws_InvalidAmount()
    {
        await Assert.ThrowsAsync<InvalidAmountException>(() => _handler.Handle(new DepositCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 0m,
            Provider = "stripe"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task When_provider_is_unknown_deposit_throws_UnknownProvider()
    {
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Alice());
        _factory.Setup(factory => factory.Create("bitcoin"))
            .Throws(new UnknownProviderException("bitcoin"));

        await Assert.ThrowsAsync<UnknownProviderException>(() => _handler.Handle(new DepositCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 10.00m,
            Provider = "bitcoin"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task When_provider_declines_star13_amount_deposit_does_not_write()
    {
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Alice());
        _factory.Setup(factory => factory.Create("stripe")).Returns(_provider.Object);
        _provider.Setup(provider => provider.DepositAsync(SeedData.AliceId, 10.13m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderResult { IsApproved = false, DeclineReason = "declined" });

        await Assert.ThrowsAsync<ProviderDeclinedException>(() => _handler.Handle(new DepositCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 10.13m,
            Provider = "stripe"
        }, CancellationToken.None));

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transactions.Verify(repository => repository.AddAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Customer Alice() => new()
    {
        Id = SeedData.AliceId,
        Name = "Alice",
        Balance = 100.00m
    };
}

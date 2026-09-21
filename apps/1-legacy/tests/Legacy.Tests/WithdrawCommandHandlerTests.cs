using Legacy.Application.Commands;
using Legacy.Domain;
using Legacy.Domain.Entities;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Providers;
using Legacy.Domain.Repositories;
using Moq;

namespace Legacy.Tests;

public class WithdrawCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IPaymentTransactionRepository> _transactions = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPaymentProviderFactory> _factory = new();
    private readonly Mock<IPaymentProvider> _provider = new();
    private readonly WithdrawCommandHandler _handler;

    public WithdrawCommandHandlerTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Customers).Returns(_customers.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.Transactions).Returns(_transactions.Object);
        _provider.Setup(provider => provider.Name).Returns("bank");
        _handler = new WithdrawCommandHandler(_unitOfWork.Object, _factory.Object);
    }

    [Fact]
    public async Task When_withdraw_succeeds_balance_is_updated_and_transaction_is_written()
    {
        var customer = Alice();
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _factory.Setup(factory => factory.Create("bank")).Returns(_provider.Object);
        _provider.Setup(provider => provider.WithdrawAsync(SeedData.AliceId, 40.00m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderResult { IsApproved = true, ExternalReference = "bank-ref" });

        var result = await _handler.Handle(new WithdrawCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 40.00m,
            Provider = "bank"
        }, CancellationToken.None);

        Assert.Equal(60.00m, result.Balance);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task When_account_has_insufficient_funds_withdrawal_is_rejected()
    {
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.BobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = SeedData.BobId, Name = "Bob", Balance = 25.00m });
        _factory.Setup(factory => factory.Create("stripe")).Returns(_provider.Object);

        await Assert.ThrowsAsync<InsufficientFundsException>(() => _handler.Handle(new WithdrawCommand
        {
            CustomerId = SeedData.BobId,
            Amount = 40.00m,
            Provider = "stripe"
        }, CancellationToken.None));

        _provider.Verify(
            provider => provider.WithdrawAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task When_daily_limit_is_set_and_withdrawal_is_within_remaining_limit_it_succeeds()
    {
        var customer = Alice();
        customer.DailyWithdrawalLimit = 50.00m;
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _transactions.Setup(repository => repository.SumWithdrawalsSinceAsync(
                SeedData.AliceId,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10.00m);
        _factory.Setup(factory => factory.Create("bank")).Returns(_provider.Object);
        _provider.Setup(provider => provider.WithdrawAsync(SeedData.AliceId, 40.00m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderResult { IsApproved = true, ExternalReference = "bank-ref" });

        var result = await _handler.Handle(new WithdrawCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 40.00m,
            Provider = "bank"
        }, CancellationToken.None);

        Assert.Equal(60.00m, result.Balance);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task When_withdrawal_would_exceed_daily_limit_it_is_rejected()
    {
        var customer = Alice();
        customer.DailyWithdrawalLimit = 50.00m;
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _transactions.Setup(repository => repository.SumWithdrawalsSinceAsync(
                SeedData.AliceId,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(20.00m);
        _factory.Setup(factory => factory.Create("bank")).Returns(_provider.Object);

        await Assert.ThrowsAsync<DailyLimitExceededException>(() => _handler.Handle(new WithdrawCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 40.00m,
            Provider = "bank"
        }, CancellationToken.None));

        _provider.Verify(
            provider => provider.WithdrawAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Customer Alice() => new()
    {
        Id = SeedData.AliceId,
        Name = "Alice",
        Balance = 100.00m
    };
}

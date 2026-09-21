using Legacy.Application.Commands;
using Legacy.Domain;
using Legacy.Domain.Entities;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Repositories;
using Moq;

namespace Legacy.Tests;

public class SetDailyWithdrawalLimitCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SetDailyWithdrawalLimitCommandHandler _handler;

    public SetDailyWithdrawalLimitCommandHandlerTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Customers).Returns(_customers.Object);
        _handler = new SetDailyWithdrawalLimitCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task When_setting_a_valid_limit_it_is_persisted()
    {
        var customer = new Customer { Id = SeedData.AliceId, Name = "Alice", Balance = 100.00m };
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(new SetDailyWithdrawalLimitCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 50.00m
        }, CancellationToken.None);

        Assert.Equal(50.00m, result.DailyWithdrawalLimit);
        Assert.Equal(50.00m, customer.DailyWithdrawalLimit);
        _customers.Verify(repository => repository.Update(customer), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task When_changing_an_existing_limit_the_new_amount_is_persisted()
    {
        var customer = new Customer
        {
            Id = SeedData.AliceId,
            Name = "Alice",
            Balance = 100.00m,
            DailyWithdrawalLimit = 50.00m
        };
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(new SetDailyWithdrawalLimitCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 75.00m
        }, CancellationToken.None);

        Assert.Equal(75.00m, result.DailyWithdrawalLimit);
        Assert.Equal(75.00m, customer.DailyWithdrawalLimit);
    }

    [Fact]
    public async Task When_amount_is_invalid_set_limit_throws_InvalidAmount()
    {
        await Assert.ThrowsAsync<InvalidAmountException>(() => _handler.Handle(new SetDailyWithdrawalLimitCommand
        {
            CustomerId = SeedData.AliceId,
            Amount = 0m
        }, CancellationToken.None));

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task When_customer_is_unknown_set_limit_throws_AccountNotFound()
    {
        _customers.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<AccountNotFoundException>(() => _handler.Handle(new SetDailyWithdrawalLimitCommand
        {
            CustomerId = Guid.NewGuid(),
            Amount = 50.00m
        }, CancellationToken.None));
    }
}

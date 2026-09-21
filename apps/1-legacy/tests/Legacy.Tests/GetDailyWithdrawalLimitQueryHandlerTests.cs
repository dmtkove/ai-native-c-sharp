using Legacy.Application.Queries;
using Legacy.Domain;
using Legacy.Domain.Entities;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Repositories;
using Moq;

namespace Legacy.Tests;

public class GetDailyWithdrawalLimitQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly GetDailyWithdrawalLimitQueryHandler _handler;

    public GetDailyWithdrawalLimitQueryHandlerTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Customers).Returns(_customers.Object);
        _handler = new GetDailyWithdrawalLimitQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task When_no_limit_is_set_null_is_returned()
    {
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = SeedData.AliceId, Name = "Alice", Balance = 100.00m });

        var result = await _handler.Handle(
            new GetDailyWithdrawalLimitQuery { CustomerId = SeedData.AliceId },
            CancellationToken.None);

        Assert.Equal(SeedData.AliceId, result.CustomerId);
        Assert.Null(result.DailyWithdrawalLimit);
    }

    [Fact]
    public async Task When_a_limit_is_set_the_amount_is_returned()
    {
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer
            {
                Id = SeedData.AliceId,
                Name = "Alice",
                Balance = 100.00m,
                DailyWithdrawalLimit = 80.00m
            });

        var result = await _handler.Handle(
            new GetDailyWithdrawalLimitQuery { CustomerId = SeedData.AliceId },
            CancellationToken.None);

        Assert.Equal(80.00m, result.DailyWithdrawalLimit);
    }

    [Fact]
    public async Task When_customer_is_unknown_get_limit_throws_AccountNotFound()
    {
        _customers.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            _handler.Handle(new GetDailyWithdrawalLimitQuery { CustomerId = Guid.NewGuid() }, CancellationToken.None));
    }
}

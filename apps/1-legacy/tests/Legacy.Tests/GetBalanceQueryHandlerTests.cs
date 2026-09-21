using Legacy.Application.Queries;
using Legacy.Domain;
using Legacy.Domain.Entities;
using Legacy.Domain.Exceptions;
using Legacy.Domain.Repositories;
using Moq;

namespace Legacy.Tests;

public class GetBalanceQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly GetBalanceQueryHandler _handler;

    public GetBalanceQueryHandlerTests()
    {
        _unitOfWork.Setup(unitOfWork => unitOfWork.Customers).Returns(_customers.Object);
        _handler = new GetBalanceQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task When_getting_balance_for_seeded_customer_current_balance_is_returned()
    {
        _customers.Setup(repository => repository.GetByIdAsync(SeedData.AliceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = SeedData.AliceId, Name = "Alice", Balance = 100.00m });

        var result = await _handler.Handle(new GetBalanceQuery { CustomerId = SeedData.AliceId }, CancellationToken.None);

        Assert.Equal(SeedData.AliceId, result.CustomerId);
        Assert.Equal(100.00m, result.Balance);
    }

    [Fact]
    public async Task When_customer_is_unknown_get_balance_throws_AccountNotFound()
    {
        _customers.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            _handler.Handle(new GetBalanceQuery { CustomerId = Guid.NewGuid() }, CancellationToken.None));
    }
}

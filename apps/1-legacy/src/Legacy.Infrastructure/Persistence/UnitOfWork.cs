using Legacy.Domain.Repositories;

namespace Legacy.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(
        AppDbContext dbContext,
        ICustomerRepository customers,
        IPaymentTransactionRepository transactions)
    {
        _dbContext = dbContext;
        Customers = customers;
        Transactions = transactions;
    }

    public ICustomerRepository Customers { get; }

    public IPaymentTransactionRepository Transactions { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

namespace Legacy.Domain.Repositories;

public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }

    IPaymentTransactionRepository Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

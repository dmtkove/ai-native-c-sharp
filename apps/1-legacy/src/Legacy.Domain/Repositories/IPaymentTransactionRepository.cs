using Legacy.Domain.Entities;

namespace Legacy.Domain.Repositories;

public interface IPaymentTransactionRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);

    Task<decimal> SumWithdrawalsSinceAsync(Guid customerId, DateTimeOffset fromInclusive, CancellationToken cancellationToken = default);
}

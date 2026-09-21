using Legacy.Domain.Entities;
using Legacy.Domain.Enums;
using Legacy.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Infrastructure.Persistence;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentTransactionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<decimal> SumWithdrawalsSinceAsync(Guid customerId, DateTimeOffset fromInclusive, CancellationToken cancellationToken = default)
    {
        // SQLite cannot translate DateTimeOffset comparisons; filter the day in memory.
        var rows = await _dbContext.Transactions
            .Where(transaction =>
                transaction.CustomerId == customerId
                && transaction.Type == TransactionType.Withdrawal)
            .Select(transaction => new { transaction.Amount, transaction.CreatedAt })
            .ToListAsync(cancellationToken);

        return rows
            .Where(transaction => transaction.CreatedAt >= fromInclusive)
            .Sum(transaction => transaction.Amount);
    }
}

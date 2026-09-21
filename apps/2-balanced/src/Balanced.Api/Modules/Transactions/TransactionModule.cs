using Balanced.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Balanced.Api.Modules.Transactions;

public sealed class TransactionModule(AppDbContext db)
{
    public Guid Record(
        Guid customerId,
        string type,
        decimal amount,
        string provider,
        string externalReference,
        DateTimeOffset createdAt)
    {
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = type,
            Amount = amount,
            Provider = provider,
            ExternalReference = externalReference,
            CreatedAt = createdAt
        };

        db.Transactions.Add(transaction);
        return transaction.Id;
    }

    public async Task<decimal> SumWithdrawalsSinceAsync(Guid customerId, DateTimeOffset fromInclusive, CancellationToken cancellationToken)
    {
        // SQLite cannot translate DateTimeOffset comparisons; filter the day in memory.
        var rows = await db.Transactions
            .Where(transaction =>
                transaction.CustomerId == customerId
                && transaction.Type == "Withdrawal")
            .Select(transaction => new { transaction.Amount, transaction.CreatedAt })
            .ToListAsync(cancellationToken);

        return rows
            .Where(transaction => transaction.CreatedAt >= fromInclusive)
            .Sum(transaction => transaction.Amount);
    }
}

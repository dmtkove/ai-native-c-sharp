using Balanced.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Balanced.Api.Modules.Accounts;

public sealed class AccountModule(AppDbContext db)
{
    public Task<Customer?> GetAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return db.Customers.FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);
    }

    public void Credit(Customer customer, Money amount)
    {
        customer.Balance += amount.Amount;
    }

    public void Debit(Customer customer, Money amount)
    {
        customer.Balance -= amount.Amount;
    }

    public void SetDailyWithdrawalLimit(Customer customer, Money amount)
    {
        customer.DailyWithdrawalLimit = amount.Amount;
    }
}

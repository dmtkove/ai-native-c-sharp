using Balanced.Api.Data;
using Balanced.Api.Modules.Accounts;

namespace Balanced.Api.Features.DailyWithdrawalLimit;

public sealed class SetDailyWithdrawalLimitHandler(AccountModule accounts, AppDbContext db)
{
    public async Task<DailyWithdrawalLimitResponse> Handle(
        Guid customerId,
        SetDailyWithdrawalLimitRequest request,
        CancellationToken cancellationToken)
    {
        var amount = new Money(request.Amount);
        if (!amount.IsPositive)
        {
            return new DailyWithdrawalLimitResponse(
                OperationStatus.InvalidAmount,
                customerId,
                null,
                "Amount must be greater than zero.");
        }

        var customer = await accounts.GetAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return new DailyWithdrawalLimitResponse(
                OperationStatus.AccountNotFound,
                customerId,
                null,
                "Customer was not found.");
        }

        accounts.SetDailyWithdrawalLimit(customer, amount);
        await db.SaveChangesAsync(cancellationToken);

        return new DailyWithdrawalLimitResponse(
            OperationStatus.Success,
            customer.Id,
            customer.DailyWithdrawalLimit,
            null);
    }
}

using Balanced.Api.Data;
using Balanced.Api.Modules.Accounts;

namespace Balanced.Api.Features.DailyWithdrawalLimit;

public sealed class GetDailyWithdrawalLimitHandler(AccountModule accounts)
{
    public async Task<DailyWithdrawalLimitResponse> Handle(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await accounts.GetAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return new DailyWithdrawalLimitResponse(
                OperationStatus.AccountNotFound,
                customerId,
                null,
                "Customer was not found.");
        }

        return new DailyWithdrawalLimitResponse(
            OperationStatus.Success,
            customer.Id,
            customer.DailyWithdrawalLimit,
            null);
    }
}

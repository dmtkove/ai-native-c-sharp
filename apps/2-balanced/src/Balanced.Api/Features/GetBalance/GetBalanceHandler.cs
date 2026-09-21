using Balanced.Api.Modules.Accounts;

namespace Balanced.Api.Features.GetBalance;

public sealed class GetBalanceHandler(AccountModule accounts)
{
    public async Task<BalanceResponse> Handle(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await accounts.GetAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return new BalanceResponse(OperationStatus.AccountNotFound, customerId, null, "Customer was not found.");
        }

        return new BalanceResponse(OperationStatus.Success, customer.Id, customer.Balance, null);
    }
}

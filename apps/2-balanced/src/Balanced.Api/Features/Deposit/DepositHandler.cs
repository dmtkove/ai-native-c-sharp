using Balanced.Api.Data;
using Balanced.Api.Modules.Accounts;
using Balanced.Api.Modules.Transactions;
using Balanced.Api.Providers;

namespace Balanced.Api.Features.Deposit;

public sealed class DepositHandler(
    AccountModule accounts,
    TransactionModule transactions,
    AppDbContext db)
{
    public async Task<PaymentResponse> Handle(PaymentRequest request, CancellationToken cancellationToken)
    {
        var amount = new Money(request.Amount);
        if (!amount.IsPositive)
        {
            return PaymentResponse.Fail(OperationStatus.InvalidAmount, "Amount must be greater than zero.");
        }

        var customer = await accounts.GetAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return PaymentResponse.Fail(OperationStatus.AccountNotFound, "Customer was not found.", request.CustomerId);
        }

        var providerResult = PaymentProvider.Execute(request.Provider, amount.Amount);
        if (providerResult.Status != OperationStatus.Success)
        {
            return PaymentResponse.Fail(providerResult.Status, StatusMessage(providerResult.Status), customer.Id, customer.Balance);
        }

        accounts.Credit(customer, amount);
        var transactionId = transactions.Record(
            customer.Id,
            type: "Deposit",
            amount.Amount,
            request.Provider.ToLowerInvariant(),
            providerResult.ExternalReference!,
            DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        return PaymentResponse.Ok(customer.Id, customer.Balance, transactionId);
    }

    private static string StatusMessage(OperationStatus status) => status switch
    {
        OperationStatus.UnknownProvider => "Payment provider is not supported.",
        OperationStatus.ProviderDeclined => "The payment provider declined the transaction.",
        _ => status.ToString()
    };
}

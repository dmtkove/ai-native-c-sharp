using Balanced.Api.Data;
using Balanced.Api.Modules.Accounts;
using Balanced.Api.Modules.Transactions;
using Balanced.Api.Providers;

namespace Balanced.Api.Features.Withdraw;

public sealed class WithdrawHandler(
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

        if (!PaymentProvider.IsKnown(request.Provider))
        {
            return PaymentResponse.Fail(OperationStatus.UnknownProvider, "Payment provider is not supported.", customer.Id, customer.Balance);
        }

        if (customer.Balance < amount.Amount)
        {
            return PaymentResponse.Fail(
                OperationStatus.InsufficientFunds,
                "Balance is less than the requested amount.",
                customer.Id,
                customer.Balance);
        }

        if (customer.DailyWithdrawalLimit is decimal dailyLimit)
        {
            var startOfUtcDay = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
            var withdrawnToday = await transactions.SumWithdrawalsSinceAsync(customer.Id, startOfUtcDay, cancellationToken);
            if (withdrawnToday + amount.Amount > dailyLimit)
            {
                return PaymentResponse.Fail(
                    OperationStatus.DailyLimitExceeded,
                    "Withdrawal would exceed the daily withdrawal limit.",
                    customer.Id,
                    customer.Balance);
            }
        }

        var providerResult = PaymentProvider.Execute(request.Provider, amount.Amount);
        if (providerResult.Status != OperationStatus.Success)
        {
            return PaymentResponse.Fail(
                providerResult.Status,
                "The payment provider declined the transaction.",
                customer.Id,
                customer.Balance);
        }

        accounts.Debit(customer, amount);
        var transactionId = transactions.Record(
            customer.Id,
            type: "Withdrawal",
            amount.Amount,
            request.Provider.ToLowerInvariant(),
            providerResult.ExternalReference!,
            DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        return PaymentResponse.Ok(customer.Id, customer.Balance, transactionId);
    }
}

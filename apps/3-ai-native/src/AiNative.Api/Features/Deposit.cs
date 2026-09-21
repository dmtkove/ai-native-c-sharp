// Deposit — POST /deposits
//
// Happy path:
//   { "customerId": "11111111-1111-1111-1111-111111111111", "amount": 10.00, "provider": "stripe" }
//   { "status": "Success", "customerId": "...", "balance": 110.00, "transactionId": "...", "errorMessage": null }
//
// Statuses:
//   InvalidAmount     amount <= 0
//   AccountNotFound   customerId is not in Customers
//   UnknownProvider   provider is not stripe|paypal|bank
//   ProviderDeclined  amount fractional part is 0.13 (dummy provider rule)
//   Success           credit balance, insert Transactions row type=Deposit
//
// Sibling: Features/Withdraw.cs uses the same dummy-provider decline rule.

using AiNative.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AiNative.Api;

public static class Deposit
{
    public sealed record Request(Guid CustomerId, decimal Amount, string Provider);

    public sealed record Response(
        OperationStatus Status,
        Guid? CustomerId,
        decimal? Balance,
        Guid? TransactionId,
        string? ErrorMessage);

    public abstract record Outcome
    {
        public sealed record Ok(CustomerId CustomerId, Money Balance, Guid TransactionId) : Outcome;

        public sealed record Fail(
            OperationStatus Status,
            string ErrorMessage,
            CustomerId? CustomerId = null,
            Money? Balance = null) : Outcome;
    }

    public static async Task<IResult> Handle(Request request, AppDbContext db, DateTimeOffset now)
    {
        var outcome = await Execute(request, db, now);
        return outcome switch
        {
            Outcome.Ok ok => Results.Ok(new Response(
                OperationStatus.Success,
                ok.CustomerId.Value,
                ok.Balance.Amount,
                ok.TransactionId,
                null)),
            Outcome.Fail fail => Results.Ok(new Response(
                fail.Status,
                fail.CustomerId?.Value,
                fail.Balance?.Amount,
                null,
                fail.ErrorMessage)),
            _ => throw new InvalidOperationException($"Unhandled deposit outcome: {outcome.GetType().Name}")
        };
    }

    public static async Task<Outcome> Execute(Request request, AppDbContext db, DateTimeOffset now)
    {
        var amount = new Money(request.Amount);
        if (!amount.IsPositive)
        {
            return new Outcome.Fail(OperationStatus.InvalidAmount, "Amount must be greater than zero.");
        }

        var customerId = new CustomerId(request.CustomerId);
        var customer = await db.Customers.FirstOrDefaultAsync(row => row.Id == customerId.Value);
        if (customer is null)
        {
            return new Outcome.Fail(OperationStatus.AccountNotFound, "Customer was not found.", customerId);
        }

        if (!ProviderId.TryParse(request.Provider, out var provider))
        {
            return new Outcome.Fail(
                OperationStatus.UnknownProvider,
                "Payment provider is not supported.",
                customerId,
                new Money(customer.Balance));
        }

        // Copied on purpose from Withdraw.cs — keep the decline rule next to this operation.
        if (amount.IsProviderDeclineAmount)
        {
            return new Outcome.Fail(
                OperationStatus.ProviderDeclined,
                "The payment provider declined the transaction.",
                customerId,
                new Money(customer.Balance));
        }

        var externalReference = $"{provider.Value}-{Guid.NewGuid():N}";

        // Explicit state transition: credit the accepted amount.
        customer.Balance += amount.Amount;

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Type = "Deposit",
            Amount = amount.Amount,
            Provider = provider.Value,
            CreatedAt = now,
            ExternalReference = externalReference
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        return new Outcome.Ok(customerId, new Money(customer.Balance), transaction.Id);
    }
}

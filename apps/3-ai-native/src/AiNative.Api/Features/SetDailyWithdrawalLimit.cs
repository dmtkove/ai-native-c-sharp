// SetDailyWithdrawalLimit — PUT /customers/{customerId}/daily-withdrawal-limit
//
// Happy path:
//   PUT /customers/11111111-1111-1111-1111-111111111111/daily-withdrawal-limit
//   { "amount": 50.00 }
//   { "status": "Success", "customerId": "...", "dailyWithdrawalLimit": 50.00, "errorMessage": null }
//
// Statuses:
//   InvalidAmount     amount <= 0
//   AccountNotFound   customerId is not in Customers
//   Success           persist Customers.DailyWithdrawalLimit
//
// Sibling: Features/GetDailyWithdrawalLimit.cs reads the same column.
// Sibling: Features/Withdraw.cs enforces the limit on the UTC calendar day.

using AiNative.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AiNative.Api;

public static class SetDailyWithdrawalLimit
{
    public sealed record Request(decimal Amount);

    public sealed record Response(
        OperationStatus Status,
        Guid? CustomerId,
        decimal? DailyWithdrawalLimit,
        string? ErrorMessage);

    public abstract record Outcome
    {
        public sealed record Ok(CustomerId CustomerId, Money DailyWithdrawalLimit) : Outcome;

        public sealed record Fail(OperationStatus Status, string ErrorMessage, CustomerId? CustomerId = null) : Outcome;
    }

    public static async Task<IResult> Handle(Guid customerIdValue, Request request, AppDbContext db)
    {
        var outcome = await Execute(new CustomerId(customerIdValue), request, db);
        return outcome switch
        {
            Outcome.Ok ok => Results.Ok(new Response(
                OperationStatus.Success,
                ok.CustomerId.Value,
                ok.DailyWithdrawalLimit.Amount,
                null)),
            Outcome.Fail fail => Results.Ok(new Response(
                fail.Status,
                fail.CustomerId?.Value,
                null,
                fail.ErrorMessage)),
            _ => throw new InvalidOperationException($"Unhandled set-daily-withdrawal-limit outcome: {outcome.GetType().Name}")
        };
    }

    public static async Task<Outcome> Execute(CustomerId customerId, Request request, AppDbContext db)
    {
        var amount = new Money(request.Amount);
        if (!amount.IsPositive)
        {
            return new Outcome.Fail(OperationStatus.InvalidAmount, "Amount must be greater than zero.", customerId);
        }

        var customer = await db.Customers.FirstOrDefaultAsync(row => row.Id == customerId.Value);
        if (customer is null)
        {
            return new Outcome.Fail(OperationStatus.AccountNotFound, "Customer was not found.", customerId);
        }

        customer.DailyWithdrawalLimit = amount.Amount;
        await db.SaveChangesAsync();

        return new Outcome.Ok(customerId, amount);
    }
}

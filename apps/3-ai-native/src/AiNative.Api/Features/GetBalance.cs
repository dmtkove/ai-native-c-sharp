// GetBalance — GET /customers/{customerId}/balance
//
// Happy path:
//   GET /customers/11111111-1111-1111-1111-111111111111/balance
//   { "status": "Success", "customerId": "...", "balance": 100.00, "errorMessage": null }
//
// Statuses:
//   AccountNotFound   customerId is not in Customers
//   Success           current Customers.Balance

using AiNative.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AiNative.Api;

public static class GetBalance
{
    public sealed record Response(
        OperationStatus Status,
        Guid? CustomerId,
        decimal? Balance,
        string? ErrorMessage);

    public abstract record Outcome
    {
        public sealed record Ok(CustomerId CustomerId, Money Balance) : Outcome;

        public sealed record Fail(OperationStatus Status, string ErrorMessage, CustomerId? CustomerId = null) : Outcome;
    }

    public static async Task<IResult> Handle(Guid customerIdValue, AppDbContext db)
    {
        var outcome = await Execute(new CustomerId(customerIdValue), db);
        return outcome switch
        {
            Outcome.Ok ok => Results.Ok(new Response(
                OperationStatus.Success,
                ok.CustomerId.Value,
                ok.Balance.Amount,
                null)),
            Outcome.Fail fail => Results.Ok(new Response(
                fail.Status,
                fail.CustomerId?.Value,
                null,
                fail.ErrorMessage)),
            _ => throw new InvalidOperationException($"Unhandled get-balance outcome: {outcome.GetType().Name}")
        };
    }

    public static async Task<Outcome> Execute(CustomerId customerId, AppDbContext db)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(row => row.Id == customerId.Value);
        if (customer is null)
        {
            return new Outcome.Fail(OperationStatus.AccountNotFound, "Customer was not found.", customerId);
        }

        return new Outcome.Ok(customerId, new Money(customer.Balance));
    }
}

// Copy this file to Features/<Name>.cs, then map the route in Program.cs.
// Keep request, response, outcome, validation, EF, provider rules, and HTTP mapping in that one file.
// Do not add MediatR, repositories, interfaces, or constructor DI.
//
// JSON examples go in the header comment (happy path + each status).
// Update FEATURE-MAP.md in the same change.

using AiNative.Api.Data;

namespace AiNative.Api;

public static class Template
{
    public sealed record Request(Guid CustomerId);

    public sealed record Response(
        OperationStatus Status,
        Guid? CustomerId,
        string? ErrorMessage);

    public abstract record Outcome
    {
        public sealed record Ok(CustomerId CustomerId) : Outcome;

        public sealed record Fail(OperationStatus Status, string ErrorMessage, CustomerId? CustomerId = null) : Outcome;
    }

    public static async Task<IResult> Handle(Request request, AppDbContext db, DateTimeOffset now)
    {
        var outcome = await Execute(request, db, now);
        return outcome switch
        {
            Outcome.Ok ok => Results.Ok(new Response(OperationStatus.Success, ok.CustomerId.Value, null)),
            Outcome.Fail fail => Results.Ok(new Response(fail.Status, fail.CustomerId?.Value, fail.ErrorMessage)),
            _ => throw new InvalidOperationException($"Unhandled outcome: {outcome.GetType().Name}")
        };
    }

    public static Task<Outcome> Execute(Request request, AppDbContext db, DateTimeOffset now)
    {
        _ = db;
        _ = now;
        return Task.FromResult<Outcome>(
            new Outcome.Fail(OperationStatus.AccountNotFound, "Replace this template with a real operation.", new CustomerId(request.CustomerId)));
    }
}

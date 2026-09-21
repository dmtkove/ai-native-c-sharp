namespace Balanced.Api.Features.GetBalance;

public sealed record BalanceResponse(
    OperationStatus Status,
    Guid? CustomerId,
    decimal? Balance,
    string? ErrorMessage);

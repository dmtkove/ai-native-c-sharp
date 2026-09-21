namespace Balanced.Api.Features.DailyWithdrawalLimit;

public sealed record DailyWithdrawalLimitResponse(
    OperationStatus Status,
    Guid? CustomerId,
    decimal? DailyWithdrawalLimit,
    string? ErrorMessage);

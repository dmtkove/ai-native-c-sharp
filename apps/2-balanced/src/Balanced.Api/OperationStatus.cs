namespace Balanced.Api;

public enum OperationStatus
{
    Success,
    AccountNotFound,
    InsufficientFunds,
    InvalidAmount,
    UnknownProvider,
    ProviderDeclined,
    DailyLimitExceeded
}

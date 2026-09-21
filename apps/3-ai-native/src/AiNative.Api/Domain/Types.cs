namespace AiNative.Api;

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

public readonly record struct CustomerId(Guid Value);

public readonly record struct Money(decimal Amount)
{
    public bool IsPositive => Amount > 0;

    // Dummy providers decline when the fractional part is 0.13 (example: 10.13).
    public bool IsProviderDeclineAmount => Amount % 1 == 0.13m;
}

public readonly record struct ProviderId
{
    public string Value { get; }

    private ProviderId(string value) => Value = value;

    public static bool TryParse(string? raw, out ProviderId provider)
    {
        var normalized = raw?.ToLowerInvariant();
        switch (normalized)
        {
            case "stripe":
            case "paypal":
            case "bank":
                provider = new ProviderId(normalized);
                return true;
            default:
                provider = default;
                return false;
        }
    }
}

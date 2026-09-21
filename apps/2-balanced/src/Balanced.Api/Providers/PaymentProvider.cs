namespace Balanced.Api.Providers;

public static class PaymentProvider
{
    public static bool IsKnown(string? provider) =>
        provider?.ToLowerInvariant() is "stripe" or "paypal" or "bank";

    public static ProviderResult Execute(string? provider, decimal amount)
    {
        switch (provider?.ToLowerInvariant())
        {
            case "stripe":
            case "paypal":
            case "bank":
                if (amount % 1 == 0.13m)
                {
                    return new ProviderResult(OperationStatus.ProviderDeclined, null);
                }

                return new ProviderResult(OperationStatus.Success, $"{provider}-{Guid.NewGuid():N}");
            default:
                return new ProviderResult(OperationStatus.UnknownProvider, null);
        }
    }
}

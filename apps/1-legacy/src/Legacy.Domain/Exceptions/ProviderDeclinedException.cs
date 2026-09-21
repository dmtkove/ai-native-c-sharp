namespace Legacy.Domain.Exceptions;

public sealed class ProviderDeclinedException : DomainException
{
    public ProviderDeclinedException(string provider, decimal amount)
        : base($"Provider '{provider}' declined the transaction for amount {amount}.")
    {
        Provider = provider;
        Amount = amount;
    }

    public string Provider { get; }

    public decimal Amount { get; }

    public override int StatusCode => 422;

    public override string ErrorCode => "ProviderDeclined";
}

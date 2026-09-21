namespace Legacy.Domain.Exceptions;

public sealed class UnknownProviderException : DomainException
{
    public UnknownProviderException(string provider)
        : base($"Payment provider '{provider}' is not registered.")
    {
        Provider = provider;
    }

    public string Provider { get; }

    public override int StatusCode => 400;

    public override string ErrorCode => "UnknownProvider";
}

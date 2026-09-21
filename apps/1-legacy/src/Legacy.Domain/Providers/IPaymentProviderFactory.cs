namespace Legacy.Domain.Providers;

public interface IPaymentProviderFactory
{
    IPaymentProvider Create(string providerName);
}

using Legacy.Domain.Exceptions;
using Legacy.Domain.Providers;
using Microsoft.Extensions.Options;

namespace Legacy.Infrastructure.Providers;

public sealed class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providers;

    public PaymentProviderFactory(
        IEnumerable<IPaymentProvider> providers,
        IOptions<PaymentProvidersOptions> options)
    {
        var enabled = new HashSet<string>(options.Value.Enabled, StringComparer.OrdinalIgnoreCase);

        _providers = providers
            .Where(provider => enabled.Contains(provider.Name))
            .ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IPaymentProvider Create(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName) || !_providers.TryGetValue(providerName, out var provider))
        {
            throw new UnknownProviderException(providerName);
        }

        return provider;
    }
}

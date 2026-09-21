namespace Legacy.Infrastructure.Providers;

public sealed class PaymentProvidersOptions
{
    public const string SectionName = "PaymentProviders";

    public string[] Enabled { get; set; } = [];
}

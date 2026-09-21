namespace Legacy.Infrastructure.Providers;

public sealed class StripePaymentProvider : DummyPaymentProviderBase
{
    public override string Name => "stripe";
}

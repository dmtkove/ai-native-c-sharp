namespace Legacy.Infrastructure.Providers;

public sealed class PayPalPaymentProvider : DummyPaymentProviderBase
{
    public override string Name => "paypal";
}

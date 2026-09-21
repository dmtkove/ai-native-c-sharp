namespace Legacy.Infrastructure.Providers;

public sealed class BankPaymentProvider : DummyPaymentProviderBase
{
    public override string Name => "bank";
}

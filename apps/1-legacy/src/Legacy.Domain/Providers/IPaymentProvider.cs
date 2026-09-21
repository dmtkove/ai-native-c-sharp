namespace Legacy.Domain.Providers;

public interface IPaymentProvider
{
    string Name { get; }

    Task<PaymentProviderResult> DepositAsync(Guid customerId, decimal amount, CancellationToken cancellationToken);

    Task<PaymentProviderResult> WithdrawAsync(Guid customerId, decimal amount, CancellationToken cancellationToken);
}

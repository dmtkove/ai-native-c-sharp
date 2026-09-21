using Legacy.Domain.Providers;

namespace Legacy.Infrastructure.Providers;

public abstract class DummyPaymentProviderBase : IPaymentProvider
{
    public abstract string Name { get; }

    public Task<PaymentProviderResult> DepositAsync(Guid customerId, decimal amount, CancellationToken cancellationToken)
    {
        return ExecuteAsync(amount);
    }

    public Task<PaymentProviderResult> WithdrawAsync(Guid customerId, decimal amount, CancellationToken cancellationToken)
    {
        return ExecuteAsync(amount);
    }

    private Task<PaymentProviderResult> ExecuteAsync(decimal amount)
    {
        if (amount % 1 == 0.13m)
        {
            return Task.FromResult(new PaymentProviderResult
            {
                IsApproved = false,
                DeclineReason = "Dummy provider decline rule: fractional part 0.13"
            });
        }

        return Task.FromResult(new PaymentProviderResult
        {
            IsApproved = true,
            ExternalReference = $"{Name}-{Guid.NewGuid():N}"
        });
    }
}

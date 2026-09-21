namespace Legacy.Domain.Exceptions;

public sealed class DailyLimitExceededException : DomainException
{
    public DailyLimitExceededException(Guid customerId, decimal limit, decimal withdrawnToday, decimal amount)
        : base($"Customer '{customerId}' would exceed daily withdrawal limit {limit} (already withdrawn {withdrawnToday}, requested {amount}).")
    {
        CustomerId = customerId;
        Limit = limit;
        WithdrawnToday = withdrawnToday;
        Amount = amount;
    }

    public Guid CustomerId { get; }

    public decimal Limit { get; }

    public decimal WithdrawnToday { get; }

    public decimal Amount { get; }

    public override int StatusCode => 422;

    public override string ErrorCode => "DailyLimitExceeded";
}

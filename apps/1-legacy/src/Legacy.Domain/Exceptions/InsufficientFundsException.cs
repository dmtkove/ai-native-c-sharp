namespace Legacy.Domain.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(Guid customerId, decimal balance, decimal amount)
        : base($"Customer '{customerId}' has balance {balance} which is less than {amount}.")
    {
        CustomerId = customerId;
        Balance = balance;
        Amount = amount;
    }

    public Guid CustomerId { get; }

    public decimal Balance { get; }

    public decimal Amount { get; }

    public override int StatusCode => 422;

    public override string ErrorCode => "InsufficientFunds";
}

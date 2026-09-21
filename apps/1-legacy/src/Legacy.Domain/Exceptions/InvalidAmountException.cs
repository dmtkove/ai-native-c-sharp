namespace Legacy.Domain.Exceptions;

public sealed class InvalidAmountException : DomainException
{
    public InvalidAmountException(decimal amount)
        : base($"Amount '{amount}' must be greater than zero.")
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public override int StatusCode => 400;

    public override string ErrorCode => "InvalidAmount";
}

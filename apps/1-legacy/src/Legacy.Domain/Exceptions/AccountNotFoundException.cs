namespace Legacy.Domain.Exceptions;

public sealed class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid customerId)
        : base($"Customer '{customerId}' was not found.")
    {
        CustomerId = customerId;
    }

    public Guid CustomerId { get; }

    public override int StatusCode => 404;

    public override string ErrorCode => "AccountNotFound";
}

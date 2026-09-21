namespace Legacy.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    public abstract int StatusCode { get; }

    public abstract string ErrorCode { get; }
}

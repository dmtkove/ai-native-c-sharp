namespace Balanced.Api.Features;

public sealed record PaymentResponse(
    OperationStatus Status,
    Guid? CustomerId,
    decimal? Balance,
    Guid? TransactionId,
    string? ErrorMessage)
{
    public static PaymentResponse Ok(Guid customerId, decimal balance, Guid transactionId) =>
        new(OperationStatus.Success, customerId, balance, transactionId, null);

    public static PaymentResponse Fail(
        OperationStatus status,
        string errorMessage,
        Guid? customerId = null,
        decimal? balance = null) =>
        new(status, customerId, balance, null, errorMessage);
}

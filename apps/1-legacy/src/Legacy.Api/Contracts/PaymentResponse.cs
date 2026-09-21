namespace Legacy.Api.Contracts;

public class PaymentResponse
{
    public Guid CustomerId { get; set; }

    public decimal Balance { get; set; }

    public Guid TransactionId { get; set; }
}

namespace Legacy.Application.Dtos;

public class PaymentOperationResultDto
{
    public Guid CustomerId { get; set; }

    public decimal Balance { get; set; }

    public Guid TransactionId { get; set; }
}

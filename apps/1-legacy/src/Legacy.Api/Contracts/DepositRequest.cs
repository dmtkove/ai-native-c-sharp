namespace Legacy.Api.Contracts;

public class DepositRequest
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;
}

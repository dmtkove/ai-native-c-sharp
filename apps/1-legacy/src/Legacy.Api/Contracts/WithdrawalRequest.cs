namespace Legacy.Api.Contracts;

public class WithdrawalRequest
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;
}

namespace Legacy.Api.Contracts;

public class BalanceResponse
{
    public Guid CustomerId { get; set; }

    public decimal Balance { get; set; }
}

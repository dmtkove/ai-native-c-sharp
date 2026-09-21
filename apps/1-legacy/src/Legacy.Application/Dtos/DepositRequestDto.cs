namespace Legacy.Application.Dtos;

public class DepositRequestDto
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;
}

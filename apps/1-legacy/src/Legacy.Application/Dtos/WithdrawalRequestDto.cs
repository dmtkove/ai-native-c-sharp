namespace Legacy.Application.Dtos;

public class WithdrawalRequestDto
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;
}

namespace Legacy.Application.Dtos;

public class SetDailyWithdrawalLimitRequestDto
{
    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }
}

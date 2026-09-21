namespace Legacy.Application.Dtos;

public class DailyWithdrawalLimitResultDto
{
    public Guid CustomerId { get; set; }

    public decimal? DailyWithdrawalLimit { get; set; }
}

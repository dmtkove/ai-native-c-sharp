namespace Legacy.Api.Contracts;

public class DailyWithdrawalLimitResponse
{
    public Guid CustomerId { get; set; }

    public decimal? DailyWithdrawalLimit { get; set; }
}

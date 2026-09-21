namespace Balanced.Api.Data;

public sealed class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public decimal? DailyWithdrawalLimit { get; set; }
}

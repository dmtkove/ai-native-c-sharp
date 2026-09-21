namespace Legacy.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public decimal? DailyWithdrawalLimit { get; set; }

    public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}

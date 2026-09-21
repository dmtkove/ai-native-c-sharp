using Legacy.Domain.Enums;

namespace Legacy.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string ExternalReference { get; set; } = string.Empty;
}

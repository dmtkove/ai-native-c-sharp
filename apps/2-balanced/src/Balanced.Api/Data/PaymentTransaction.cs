namespace Balanced.Api.Data;

public sealed class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Provider { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string ExternalReference { get; set; } = string.Empty;
}

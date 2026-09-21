namespace Legacy.Domain.Providers;

public class PaymentProviderResult
{
    public bool IsApproved { get; set; }

    public string ExternalReference { get; set; } = string.Empty;

    public string? DeclineReason { get; set; }
}

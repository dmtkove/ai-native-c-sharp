namespace Balanced.Api.Features;

public sealed record PaymentRequest(Guid CustomerId, decimal Amount, string Provider);

namespace Balanced.Api.Providers;

public sealed record ProviderResult(OperationStatus Status, string? ExternalReference);

namespace Balanced.Api;

public readonly record struct Money(decimal Amount)
{
    public bool IsPositive => Amount > 0;
}

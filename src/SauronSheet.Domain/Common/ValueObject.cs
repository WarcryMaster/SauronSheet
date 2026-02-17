namespace SauronSheet.Domain.Common;

/// <summary>
/// Base class for domain value objects.
/// Uses C# record for automatic value-based equality.
/// Immutable by design.
/// </summary>
public abstract record ValueObject
{
    // C# record provides value-based equality automatically.
    // No additional implementation needed.
}

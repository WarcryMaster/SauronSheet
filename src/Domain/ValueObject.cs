namespace Domain;

/// <summary>
/// Base class for domain value objects.
/// Value objects are compared by their attributes, not their identity.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Get the equality components used for value comparison
    /// </summary>
    public abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Equality comparison based on component values
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var valueObject = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }

    /// <summary>
    /// Get hash code based on component values
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (a, b) => HashCode.Combine(a, b?.GetHashCode() ?? 0));
    }

    /// <summary>
    /// Type-safe equality comparison
    /// </summary>
    public bool Equals(ValueObject? other) => Equals((object?)other);

    /// <summary>
    /// Check inequality
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Check equality
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return (left?.Equals(right) ?? false) || (left is null && right is null);
    }
}

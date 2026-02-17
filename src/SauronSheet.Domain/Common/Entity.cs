namespace SauronSheet.Domain.Common;

/// <summary>
/// Base class for domain entities.
/// Provides Id, CreatedAt, UpdatedAt properties and equality methods.
/// TId must be non-null (enforced by where constraint).
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Protected constructor for derived classes.
    /// Validates that TId is not null or empty (for value types like Guid).
    /// </summary>
    protected Entity(TId id)
    {
        if (id == null || EqualityComparer<TId>.Default.Equals(id, default(TId)))
            throw new ArgumentException("Entity ID cannot be null or empty.", nameof(id));

        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}

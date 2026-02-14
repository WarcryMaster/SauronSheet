namespace Domain;

/// <summary>
/// Base class for all domain entities.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public abstract class Entity<TId> where TId : notnull
{
    /// <summary>
    /// The unique identifier for the entity
    /// </summary>
    public TId Id { get; set; } = default!;

    /// <summary>
    /// The date and time when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Collection of domain events that occurred during the entity's lifecycle
    /// </summary>
    public List<IDomainEvent> DomainEvents { get; } = new();

    /// <summary>
    /// Initialize entity with ID
    /// </summary>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Parameterless constructor for ORM
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>
    /// Equality comparison based on ID
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Id.Equals(entity.Id);
    }

    /// <summary>
    /// Get hash code based on ID
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

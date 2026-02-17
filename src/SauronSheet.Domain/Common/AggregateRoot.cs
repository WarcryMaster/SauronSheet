namespace SauronSheet.Domain.Common;

/// <summary>
/// Marker base class for aggregate roots.
/// Inherits from Entity<TId> to maintain aggregate boundaries.
/// Domain events collection will be added in a future phase.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
        // TODO: Add domain events collection in future phase
        // protected List<IDomainEvent> _domainEvents = new();
    }
}

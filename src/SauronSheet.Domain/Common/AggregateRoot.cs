namespace SauronSheet.Domain.Common;

/// <summary>
/// Marker base class for aggregate roots.
/// Inherits from Entity{TId} to maintain aggregate boundaries.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }
}

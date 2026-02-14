namespace Domain;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that has happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The date and time when the event occurred
    /// </summary>
    DateTime OccurredOn { get; }
}

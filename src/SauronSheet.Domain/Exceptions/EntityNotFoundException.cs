namespace SauronSheet.Domain.Exceptions;

/// <summary>
/// Exception thrown when an expected entity is not found.
/// Stores entity name and ID for programmatic access.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId)
        : base($"Entity '{entityName}' with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}

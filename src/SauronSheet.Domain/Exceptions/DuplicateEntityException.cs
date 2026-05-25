namespace SauronSheet.Domain.Exceptions;

/// <summary>
/// Thrown by Infrastructure repository implementations when an INSERT fails
/// due to a UNIQUE constraint violation (Postgres error code 23505).
///
/// The Application layer (BankCategoryResolutionService.ResolveOrCreateAsync) catches
/// this to perform a retry-get, enabling idempotent get-or-add behaviour.
///
/// Architecture note: defined in Domain so both Application and Infrastructure
/// can reference it without introducing circular dependencies.
/// </summary>
public class DuplicateEntityException : DomainException
{
    /// <summary>
    /// Creates the exception with entity type and conflicting key for tracing.
    /// </summary>
    public DuplicateEntityException(string entityName, string conflictingKey)
        : base($"Duplicate {entityName} with normalized key '{conflictingKey}'.")
    {
    }

    public DuplicateEntityException(string message) : base(message)
    {
    }

    public DuplicateEntityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

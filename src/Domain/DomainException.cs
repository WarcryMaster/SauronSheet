namespace Domain;

/// <summary>
/// Base exception for all domain-related errors
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when a requested entity cannot be found
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with ID {id} not found")
    {
    }
}

/// <summary>
/// Thrown when a value object fails validation
/// </summary>
public class ValueObjectValidationException : DomainException
{
    public ValueObjectValidationException(string message) : base(message)
    {
    }
}

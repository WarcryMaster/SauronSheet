namespace SauronSheet.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific errors.
/// Thrown when business invariants are violated.
/// Caught in the Application layer (not in Domain).
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

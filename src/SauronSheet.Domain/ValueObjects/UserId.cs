namespace SauronSheet.Domain.ValueObjects;

using Common;
using Exceptions;

/// <summary>
/// Strong-typed user identifier value object.
/// Prevents accidental mixing of user IDs with other string values.
/// Enforces non-null, non-empty validation at construction time.
/// </summary>
public record UserId : ValueObject
{
    public string Value { get; }

    public UserId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("UserId cannot be null or empty.");

        Value = value;
    }

    public override string ToString() => Value;
}

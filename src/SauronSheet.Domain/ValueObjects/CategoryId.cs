namespace SauronSheet.Domain.ValueObjects;

using System;
using Exceptions;

/// <summary>
/// Strong-typed ID for Category aggregate root.
/// Prevents accidental mixing with other Guid IDs at compile time.
/// </summary>
public record CategoryId(Guid Value)
{
    public CategoryId() : this(Guid.Empty) =>
        throw new DomainException("CategoryId cannot be empty.");

    public static CategoryId New() => new(Guid.NewGuid());
}

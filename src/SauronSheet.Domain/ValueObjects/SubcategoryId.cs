namespace SauronSheet.Domain.ValueObjects;

using System;
using Exceptions;

/// <summary>
/// Strong-typed ID for Subcategory aggregate root.
/// Prevents accidental mixing with other Guid IDs at compile time.
/// </summary>
public record SubcategoryId(Guid Value)
{
    public SubcategoryId() : this(Guid.Empty) =>
        throw new DomainException("SubcategoryId cannot be empty.");

    public static SubcategoryId New() => new(Guid.NewGuid());
}

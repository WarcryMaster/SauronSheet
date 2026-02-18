namespace SauronSheet.Domain.ValueObjects;

using System;
using Common;

/// <summary>
/// Strong-typed value object for Category ID.
/// Prevents accidental mixing of different ID types.
/// </summary>
public record CategoryId(Guid Value)
{
    public CategoryId() : this(Guid.Empty) =>
        throw new ArgumentException("CategoryId cannot be empty.");
}

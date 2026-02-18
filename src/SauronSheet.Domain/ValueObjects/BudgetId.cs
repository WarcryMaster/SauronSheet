namespace SauronSheet.Domain.ValueObjects;

using System;
using Common;

/// <summary>
/// Strong-typed value object for Budget ID.
/// Prevents accidental mixing of different ID types.
/// </summary>
public record BudgetId(Guid Value)
{
    public BudgetId() : this(Guid.Empty) =>
        throw new ArgumentException("BudgetId cannot be empty.");
}

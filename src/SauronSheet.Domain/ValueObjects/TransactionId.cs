namespace SauronSheet.Domain.ValueObjects;

using System;
using Common;

/// <summary>
/// Strong-typed value object for Transaction ID.
/// Prevents accidental mixing of different ID types.
/// </summary>
public record TransactionId(Guid Value)
{
    public TransactionId() : this(Guid.Empty) =>
        throw new ArgumentException("TransactionId cannot be empty.");
}

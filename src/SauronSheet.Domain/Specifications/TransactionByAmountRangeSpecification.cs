namespace SauronSheet.Domain.Specifications;

using System;
using Entities;
using ValueObjects;

/// <summary>
/// Specification for transactions within an amount range.
/// Filters transactions where Amount.Amount is between min and max (inclusive).
/// </summary>
public class TransactionByAmountRangeSpecification : BaseSpecification<Transaction>
{
    public TransactionByAmountRangeSpecification(Money min, Money max)
        : base(t => t.Amount.Amount >= min.Amount && t.Amount.Amount <= max.Amount)
    {
        if (min == null)
            throw new ArgumentNullException(nameof(min));
        if (max == null)
            throw new ArgumentNullException(nameof(max));
    }
}

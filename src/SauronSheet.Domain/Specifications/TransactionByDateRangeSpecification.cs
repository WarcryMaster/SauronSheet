namespace SauronSheet.Domain.Specifications;

using System;
using Entities;
using ValueObjects;

/// <summary>
/// Specification for transactions by date range.
/// </summary>
public class TransactionByDateRangeSpecification : BaseSpecification<Transaction>
{
    public TransactionByDateRangeSpecification(DateTime startDate, DateTime endDate)
        : base(t => t.Date >= startDate && t.Date <= endDate)
    {
    }
}

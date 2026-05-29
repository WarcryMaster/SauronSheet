namespace SauronSheet.Domain.Specifications;

using System;
using Entities;
using ValueObjects;

/// <summary>
/// Specification for transactions by date range.
/// Compares using .Date (date-only) to avoid timezone-induced month drift.
/// A transaction dated 31/12 at 23:00 UTC (01/01 CET) must stay in December.
/// </summary>
public class TransactionByDateRangeSpecification : BaseSpecification<Transaction>
{
    public TransactionByDateRangeSpecification(DateTime startDate, DateTime endDate)
        : base(t => t.Date.Date >= startDate.Date && t.Date.Date <= endDate.Date)
    {
    }
}

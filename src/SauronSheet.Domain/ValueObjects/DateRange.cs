namespace SauronSheet.Domain.ValueObjects;

using System;
using Common;

/// <summary>
/// DateRange value object representing a start and end date.
/// Enforces that end date is not before start date.
/// </summary>
public record DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public DateRange()
    {
        StartDate = DateTime.MinValue;
        EndDate = DateTime.MaxValue;
    }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
}

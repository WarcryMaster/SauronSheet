namespace SauronSheet.Domain.Services;

using System;
using Entities;
using ValueObjects;

/// <summary>
/// Domain service that calculates budget metrics for a given date range.
/// Pure domain logic — no external dependencies, no repository access.
/// </summary>
public class BudgetCalculationService
{
    /// <summary>
    /// Calculates all budget metrics for a given date range and actual spending.
    /// Respects the budget's validity period (EffectiveFrom, EffectiveUntil).
    /// </summary>
    /// <param name="budget">The budget policy to calculate metrics for.</param>
    /// <param name="from">Start of the query range (inclusive).</param>
    /// <param name="to">End of the query range (inclusive).</param>
    /// <param name="spent">Total actual spending in the range.</param>
    /// <returns>A BudgetCalculationResult with all derived metrics.</returns>
    public BudgetCalculationResult Calculate(
        Budget budget,
        DateOnly from,
        DateOnly to,
        Money spent)
    {
        // Intersect with budget validity period
        DateOnly effectiveFrom = from > budget.EffectiveFrom ? from : budget.EffectiveFrom;
        DateOnly effectiveTo = budget.EffectiveUntil.HasValue && budget.EffectiveUntil.Value < to
            ? budget.EffectiveUntil.Value
            : to;

        if (effectiveFrom > effectiveTo)
        {
            return new BudgetCalculationResult(
                PeriodsElapsed: 0,
                AccumulatedLimit: new Money(0m, budget.Limit.Currency),
                Spent: spent,
                Remaining: new Money(0m, budget.Limit.Currency),
                PercentageUsed: 0m,
                StatusLevel: BudgetStatusLevel.Green);
        }

        int periodsElapsed = PeriodsElapsed(budget.PeriodGranularity, effectiveFrom, effectiveTo);
        Money accumulatedLimit = new Money(budget.Limit.Amount * periodsElapsed, budget.Limit.Currency);

        decimal percentageUsed = accumulatedLimit.IsZero
            ? 0m
            : Math.Round(spent.Amount / accumulatedLimit.Amount * 100m, 2);

        Money remaining = accumulatedLimit.Minus(spent);

        BudgetStatusLevel statusLevel = percentageUsed < 75m ? BudgetStatusLevel.Green
            : percentageUsed < 100m ? BudgetStatusLevel.Yellow
            : percentageUsed == 100m ? BudgetStatusLevel.Red
            : BudgetStatusLevel.Overage;

        return new BudgetCalculationResult(
            PeriodsElapsed: periodsElapsed,
            AccumulatedLimit: accumulatedLimit,
            Spent: spent,
            Remaining: remaining,
            PercentageUsed: percentageUsed,
            StatusLevel: statusLevel);
    }

    /// <summary>
    /// Returns the date range boundaries for the current period of a given granularity,
    /// relative to a reference date (typically "now").
    /// E.g., for Monthly on May 15 → May 1 – May 31;
    /// for Quarterly on May 15 → Apr 1 – Jun 30;
    /// for Semester on May 15 → Jan 1 – Jun 30;
    /// for Annual on any date → Jan 1 – Dec 31.
    /// </summary>
    /// <param name="granularity">Time granularity for the period.</param>
    /// <param name="referenceDate">The date to calculate the current period for.</param>
    /// <returns>A tuple with the inclusive start and end dates of the current period.</returns>
    public static (DateOnly From, DateOnly To) GetCurrentPeriodRange(
        BudgetPeriod granularity, DateOnly referenceDate)
    {
        return granularity switch
        {
            BudgetPeriod.Monthly => GetCurrentMonth(referenceDate),
            BudgetPeriod.Quarterly => GetCurrentQuarter(referenceDate),
            BudgetPeriod.Semester => GetCurrentSemester(referenceDate),
            BudgetPeriod.Annual => (
                new DateOnly(referenceDate.Year, 1, 1),
                new DateOnly(referenceDate.Year, 12, 31)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(granularity), granularity, "Unknown budget period.")
        };
    }

    private static (DateOnly From, DateOnly To) GetCurrentMonth(DateOnly referenceDate)
    {
        return (
            new DateOnly(referenceDate.Year, referenceDate.Month, 1),
            new DateOnly(referenceDate.Year, referenceDate.Month,
                DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month)));
    }

    private static (DateOnly From, DateOnly To) GetCurrentQuarter(DateOnly referenceDate)
    {
        int quarterStartMonth = ((referenceDate.Month - 1) / 3) * 3 + 1;
        int quarterEndMonth = quarterStartMonth + 2;
        return (
            new DateOnly(referenceDate.Year, quarterStartMonth, 1),
            new DateOnly(referenceDate.Year, quarterEndMonth,
                DateTime.DaysInMonth(referenceDate.Year, quarterEndMonth)));
    }

    private static (DateOnly From, DateOnly To) GetCurrentSemester(DateOnly referenceDate)
    {
        int semesterStartMonth = ((referenceDate.Month - 1) / 6) * 6 + 1;
        int semesterEndMonth = semesterStartMonth + 5;
        return (
            new DateOnly(referenceDate.Year, semesterStartMonth, 1),
            new DateOnly(referenceDate.Year, semesterEndMonth,
                DateTime.DaysInMonth(referenceDate.Year, semesterEndMonth)));
    }

    /// <summary>
    /// Counts the number of complete periods of the given granularity
    /// within the date range. Partial periods count as complete.
    /// </summary>
    /// <param name="granularity">Time granularity for counting periods.</param>
    /// <param name="from">Start of the range (inclusive).</param>
    /// <param name="to">End of the range (inclusive).</param>
    /// <returns>Number of periods touched by the range.</returns>
    public int PeriodsElapsed(BudgetPeriod granularity, DateOnly from, DateOnly to)
    {
        return granularity switch
        {
            BudgetPeriod.Monthly => MonthsElapsed(from, to),
            BudgetPeriod.Quarterly => QuartersElapsed(from, to),
            BudgetPeriod.Semester => SemestersElapsed(from, to),
            BudgetPeriod.Annual => YearsElapsed(from, to),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "Unknown budget period.")
        };
    }

    private static int MonthsElapsed(DateOnly from, DateOnly to)
    {
        return (to.Year - from.Year) * 12 + (to.Month - from.Month) + 1;
    }

    private static int QuartersElapsed(DateOnly from, DateOnly to)
    {
        int fromQuarterIndex = from.Year * 4 + (from.Month - 1) / 3;
        int toQuarterIndex = to.Year * 4 + (to.Month - 1) / 3;
        return toQuarterIndex - fromQuarterIndex + 1;
    }

    private static int SemestersElapsed(DateOnly from, DateOnly to)
    {
        int fromSemesterIndex = from.Year * 2 + (from.Month - 1) / 6;
        int toSemesterIndex = to.Year * 2 + (to.Month - 1) / 6;
        return toSemesterIndex - fromSemesterIndex + 1;
    }

    private static int YearsElapsed(DateOnly from, DateOnly to)
    {
        return to.Year - from.Year + 1;
    }
}

namespace SauronSheet.Domain.Services;

using ValueObjects;

/// <summary>
/// Immutable result of budget calculation for a given date range.
/// Contains all derived metrics and the traffic-light status.
/// </summary>
/// <param name="PeriodsElapsed">Number of complete periods of the budget granularity within the range.</param>
/// <param name="AccumulatedLimit">Total budget limit accumulated over the elapsed periods (Limit × PeriodsElapsed).</param>
/// <param name="Spent">Total actual spending in the range.</param>
/// <param name="Remaining">AccumulatedLimit minus Spent (can be negative for overspending).</param>
/// <param name="PercentageUsed">Percentage of accumulated limit consumed: (Spent ÷ AccumulatedLimit) × 100. 0 when AccumulatedLimit is zero.</param>
/// <param name="StatusLevel">Traffic-light status (Green, Yellow, Red, Overage) based on PercentageUsed thresholds.</param>
public record BudgetCalculationResult(
    int PeriodsElapsed,
    Money AccumulatedLimit,
    Money Spent,
    Money Remaining,
    decimal PercentageUsed,
    BudgetStatusLevel StatusLevel);

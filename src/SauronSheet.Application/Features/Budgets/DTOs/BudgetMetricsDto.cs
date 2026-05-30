namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Budget metrics for a given date range.
/// Includes calculated fields from BudgetCalculationService.
/// Slice 5 — Budget redesign.
/// </summary>
public record BudgetMetricsDto(
    Guid BudgetId,
    Guid CategoryId,
    string CategoryName,
    int PeriodsElapsed,
    decimal AccumulatedLimit,
    decimal Spent,
    decimal Remaining,
    decimal PercentageUsed,
    string StatusLevel);

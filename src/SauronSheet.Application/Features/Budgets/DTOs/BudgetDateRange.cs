namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Represents a date range for a specific budget's metric calculation.
/// Used by GetBudgetMetricsQuery to pass per-budget date ranges
/// when each budget has a different "current period" window.
/// </summary>
public record BudgetDateRange(DateOnly From, DateOnly To);

namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Budget period summary for historical views.
/// Represents one month/period in the historical timeline.
/// Slice 5 — Budget redesign.
/// </summary>
public record BudgetPeriodSummaryDto(
    string Period,
    decimal AccumulatedLimit,
    decimal Spent,
    decimal Remaining,
    string StatusLevel);

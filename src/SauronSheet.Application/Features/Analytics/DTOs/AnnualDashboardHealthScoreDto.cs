namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Health Score DTO for the annual dashboard (REQ-012).
/// 6 sub-scores with weights, total score is weighted average (0-100).
/// All score values are nullable — null means no data to compute.
/// </summary>
public record AnnualDashboardHealthScoreDto(
    decimal? Total,
    decimal? SavingsScore,
    decimal? IncomeStabilityScore,
    decimal? ExpenseStabilityScore,
    decimal? CategoryDependencyScore,
    decimal? BalanceScore,
    decimal? TrendScore,

    // Weights (constants from spec, exposed for frontend transparency)
    decimal SavingsWeight,
    decimal IncomeStabilityWeight,
    decimal ExpenseStabilityWeight,
    decimal CategoryDependencyWeight,
    decimal BalanceWeight,
    decimal TrendWeight);

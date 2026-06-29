namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Historical comparison metric for REQ-017.
/// Includes current value and diff against previous, average, best, and worst years.
/// </summary>
public record HistoricalComparisonMetricDto(
    decimal Current,
    decimal? Previous,
    decimal? PreviousDiffAbs,
    decimal? PreviousDiffPct,
    decimal? Average,
    decimal? AverageDiffAbs,
    decimal? AverageDiffPct,
    decimal? Best,
    decimal? BestDiffAbs,
    decimal? BestDiffPct,
    decimal? Worst,
    decimal? WorstDiffAbs,
    decimal? WorstDiffPct);

/// <summary>
/// Historical comparison DTO (REQ-017).
/// Null message means data is available.
/// </summary>
public record HistoricalComparisonDto(
    HistoricalComparisonMetricDto? Income,
    HistoricalComparisonMetricDto? Expense,
    HistoricalComparisonMetricDto? Savings,
    HistoricalComparisonMetricDto? SavingsRate,
    HistoricalComparisonMetricDto? Balance,
    string? Message);

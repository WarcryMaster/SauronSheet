namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Annual summary with fixed/variable breakdown for income and expenses.
/// </summary>
public record AnnualAnalysisSummaryDto(
    decimal IncomeFixed,
    decimal IncomeVariable,
    decimal IncomeTotal,
    decimal ExpenseFixed,
    decimal ExpenseVariable,
    decimal ExpenseTotal,
    decimal Net,
    string Currency)
{
    /// <summary>
    /// Number of distinct months (1-12) with at least one non-zero transaction.
    /// Defaults to 0 when no data is available.
    /// </summary>
    public int MonthsWithData { get; init; }

    /// <summary>
    /// Year-over-year percentage variation, or null when no previous year data exists.
    /// </summary>
    public YearOverYearVariationDto? Variation { get; init; }
}

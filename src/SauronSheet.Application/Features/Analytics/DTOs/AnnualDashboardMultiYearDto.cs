namespace SauronSheet.Application.Features.Analytics.DTOs;

using System.Collections.Generic;

/// <summary>
/// Multi-year comparison DTO for a single year's aggregate values (REQ-003).
/// Used inside AnnualDashboardMultiYearDto for prev/next/avg/best/worst comparisons.
/// </summary>
public record MultiYearComparisonDto(
    decimal Income,
    decimal Expense,
    decimal Savings,
    decimal Balance);

/// <summary>
/// Multi-Year Comparison DTO (REQ-003).
/// Income/expense/savings/balance arrays across all available years.
/// Highlights the selected year and provides prev/next/avg/best/worst pointers.
/// Null when only 1 year of data exists.
/// </summary>
public record AnnualDashboardMultiYearDto(
    IReadOnlyList<int> Years,
    IReadOnlyList<decimal> Incomes,
    IReadOnlyList<decimal> Expenses,
    IReadOnlyList<decimal> Savings,
    IReadOnlyList<decimal> Balances,
    int HighlightYear,
    MultiYearComparisonDto? PreviousYearValue,
    MultiYearComparisonDto? NextYearValue,
    MultiYearComparisonDto? Average,
    int? BestYear,
    int? WorstYear);

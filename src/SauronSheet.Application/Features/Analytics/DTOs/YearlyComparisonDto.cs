namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Year-over-year comparison data for a single month.
/// Includes both income and expenses for each year.
/// </summary>
public record YearlyComparisonDto(
    int Month,
    string MonthName,
    decimal Year1Income,
    decimal Year1Expenses,
    decimal Year2Income,
    decimal Year2Expenses,
    string Currency);

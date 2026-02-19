namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Year-over-year comparison data for a single month.
/// Phase 4 (US4): Bar chart data for yearly spending comparison.
/// </summary>
public record YearlyComparisonDto(
    int Month,
    string MonthName,
    decimal Year1Amount,
    decimal Year2Amount,
    decimal Difference,
    decimal? PercentageChange,
    string Currency);

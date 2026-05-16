namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Monthly spending broken down by category for stacked area charts.
/// Each record represents one category's spending in one month.
/// </summary>
public record MonthlyCategorySpendingDto(
    int Month,
    string MonthName,
    string CategoryName,
    decimal Amount);

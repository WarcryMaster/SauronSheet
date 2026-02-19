namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Spending breakdown by category with amount and percentage.
/// Phase 4 (US2): Pie chart data for category spending.
/// </summary>
public record CategorySpendingDto(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount,
    string Currency,
    decimal Percentage);

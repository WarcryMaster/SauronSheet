namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Monthly trend data for a single month within a year.
/// Phase 4 (US3): Line chart data for monthly spending trends.
/// </summary>
public record MonthlyTrendDto(
    int Year,
    int Month,
    string MonthName,
    decimal TotalExpenses,
    decimal TotalIncome,
    decimal NetAmount,
    string Currency,
    int TransactionCount);

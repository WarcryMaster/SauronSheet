namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Financial Ratios DTO for the annual dashboard (REQ-011).
/// Savings rate, averages, counts. Nullable values mean division by zero or no data.
/// </summary>
public record AnnualDashboardRatiosDto(
    decimal? SavingsRate,
    decimal? AverageMonthlyIncome,
    decimal? AverageMonthlyExpense,
    decimal? AverageMonthlySavings,
    decimal? AverageDailyExpense,
    decimal? AveragePerTransaction,
    int TransactionCount,
    decimal? AverageOperationsPerMonth);

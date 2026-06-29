namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Executive Summary DTO for the annual dashboard (REQ-001).
/// Contains income, expense, net, savings, savings rate with YoY comparison
/// and ranking across all available years.
/// All change/comparison fields are nullable — null means no previous data or division by zero.
/// </summary>
public record AnnualDashboardSummaryDto(
    // Current year values
    decimal Income,
    decimal Expense,
    decimal Net,
    decimal Savings,
    decimal SavingsRate,
    int Year,

    // Navigation context
    bool HasPreviousYear,
    bool HasNextYear,

    // Ranking across all years
    int? YearRank,
    int TotalYears,

    // Previous year values (for YoY calculation)
    decimal? PreviousYearIncome,
    decimal? PreviousYearExpense,
    decimal? PreviousYearNet,
    decimal? PreviousYearSavings,
    decimal? PreviousYearSavingsRate,

    // YoY changes — absolute and percentage
    decimal? IncomeChangeAbs,
    decimal? IncomeChangePct,
    decimal? ExpenseChangeAbs,
    decimal? ExpenseChangePct,
    decimal? NetChangeAbs,
    decimal? NetChangePct,
    decimal? SavingsChangeAbs,
    decimal? SavingsChangePct,

    // Averages across all available years
    decimal? AverageIncome,
    decimal? AverageExpense,
    decimal? AverageNet,
    decimal? AverageSavings,
    decimal? AverageSavingsRate);

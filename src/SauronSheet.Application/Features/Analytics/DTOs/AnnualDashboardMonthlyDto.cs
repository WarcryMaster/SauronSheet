namespace SauronSheet.Application.Features.Analytics.DTOs;

using System.Collections.Generic;

/// <summary>
/// Monthly Evolution DTO (REQ-004).
/// 12 months of income/expense/savings data with overlay averages
/// and best/worst month detection.
/// Month indices: 0 = January, 11 = December.
/// </summary>
public record AnnualDashboardMonthlyDto(
    IReadOnlyList<decimal> Incomes,
    IReadOnlyList<decimal> Expenses,
    IReadOnlyList<decimal> Savings,
    decimal? PreviousYearAverageIncome,
    decimal? PreviousYearAverageExpense,
    decimal? HistoricalAverageIncome,
    decimal? HistoricalAverageExpense,
    int? BestIncomeMonth,
    int? BestExpenseMonth,
    int? WorstIncomeMonth,
    int? WorstExpenseMonth);

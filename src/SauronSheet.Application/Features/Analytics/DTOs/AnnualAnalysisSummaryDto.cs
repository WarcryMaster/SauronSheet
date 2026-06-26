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
    string Currency);

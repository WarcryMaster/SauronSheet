namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.ValueObjects;
using DTOs;
using Classification;

/// <summary>
/// Pure service that computes the Executive Summary (REQ-001) from transactions.
/// No external dependencies — receives all data as parameters.
/// Uses AnnualClassificationEngine output for categorized breakdowns.
/// </summary>
public static class AnnualSummaryService
{
    /// <summary>
    /// Computes the AnnualDashboardSummaryDto from raw transactions and classification data.
    /// </summary>
    /// <param name="transactions">All year-filtered transactions for the selected year.</param>
    /// <param name="year">The selected year.</param>
    /// <param name="classifiedRows">Output from AnnualClassificationEngine.Classify().</param>
    /// <param name="subcategoryNames">Subcategory ID → name mapping.</param>
    /// <param name="minAvailableYear">Earliest available year (null = single year).</param>
    /// <param name="maxAvailableYear">Latest available year (null = single year).</param>
    /// <param name="previousYearIncome">Total income from prior year (null = no prior data).</param>
    /// <param name="previousYearExpense">Total expense from prior year.</param>
    /// <param name="previousYearNet">Net from prior year.</param>
    /// <param name="previousYearSavings">Savings from prior year.</param>
    /// <param name="previousYearSavingsRate">Savings rate from prior year.</param>
    /// <returns>AnnualDashboardSummaryDto with all computed values.</returns>
    public static AnnualDashboardSummaryDto Compute(
        IReadOnlyList<Transaction> transactions,
        int year,
        IReadOnlyList<AnnualAnalysisRowDto> classifiedRows,
        IReadOnlyDictionary<SubcategoryId, string> subcategoryNames,
        int minAvailableYear = 0,
        int maxAvailableYear = 0,
        decimal? previousYearIncome = null,
        decimal? previousYearExpense = null,
        decimal? previousYearNet = null,
        decimal? previousYearSavings = null,
        decimal? previousYearSavingsRate = null)
    {
        // Separate income and expense transactions
        List<Transaction> incomeTransactions = transactions
            .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
            .ToList();

        List<Transaction> expenseTransactions = transactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .ToList();

        // Compute aggregates from classified rows (more accurate than raw transactions)
        decimal income = classifiedRows
            .Where(r => r.IsIncome)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal expense = classifiedRows
            .Where(r => !r.IsIncome)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal net = income - expense;
        decimal savings = net; // Savings = net when all cash flow is accounted
        decimal savingsRate = income > 0m ? Math.Round(net / income * 100m, 2) : 0m;

        // Determine year navigation
        bool hasRange = minAvailableYear > 0 && maxAvailableYear >= minAvailableYear;
        bool hasPreviousYear = hasRange && minAvailableYear < year;
        bool hasNextYear = hasRange && maxAvailableYear > year;
        int totalYears = hasRange ? maxAvailableYear - minAvailableYear + 1 : 0;

        // Compute YoY changes
        decimal? incomeChangeAbs = previousYearIncome.HasValue && income > 0m
            ? Math.Round(income - previousYearIncome.Value, 2)
            : null;

        decimal? incomeChangePct = previousYearIncome.HasValue && previousYearIncome.Value != 0m
            ? Math.Round((income - previousYearIncome.Value) / Math.Abs(previousYearIncome.Value) * 100m, 2)
            : null;

        decimal? expenseChangeAbs = previousYearExpense.HasValue && expense > 0m
            ? Math.Round(expense - previousYearExpense.Value, 2)
            : null;

        decimal? expenseChangePct = previousYearExpense.HasValue && previousYearExpense.Value != 0m
            ? Math.Round((expense - previousYearExpense.Value) / Math.Abs(previousYearExpense.Value) * 100m, 2)
            : null;

        decimal? netChangeAbs = previousYearNet.HasValue
            ? Math.Round(net - previousYearNet.Value, 2)
            : null;

        decimal? netChangePct = previousYearNet.HasValue && previousYearNet.Value != 0m
            ? Math.Round((net - previousYearNet.Value) / Math.Abs(previousYearNet.Value) * 100m, 2)
            : null;

        decimal? savingsChangeAbs = previousYearSavings.HasValue
            ? Math.Round(savings - previousYearSavings.Value, 2)
            : null;

        decimal? savingsChangePct = previousYearSavings.HasValue && previousYearSavings.Value != 0m
            ? Math.Round((savings - previousYearSavings.Value) / Math.Abs(previousYearSavings.Value) * 100m, 2)
            : null;

        return new AnnualDashboardSummaryDto(
            Income: income,
            Expense: expense,
            Net: net,
            Savings: savings,
            SavingsRate: savingsRate,
            Year: year,
            HasPreviousYear: hasPreviousYear,
            HasNextYear: hasNextYear,
            YearRank: null, // Computed by handler with multi-year data
            TotalYears: totalYears,
            PreviousYearIncome: previousYearIncome,
            PreviousYearExpense: previousYearExpense,
            PreviousYearNet: previousYearNet,
            PreviousYearSavings: previousYearSavings,
            PreviousYearSavingsRate: previousYearSavingsRate,
            IncomeChangeAbs: incomeChangeAbs,
            IncomeChangePct: incomeChangePct,
            ExpenseChangeAbs: expenseChangeAbs,
            ExpenseChangePct: expenseChangePct,
            NetChangeAbs: netChangeAbs,
            NetChangePct: netChangePct,
            SavingsChangeAbs: savingsChangeAbs,
            SavingsChangePct: savingsChangePct,
            AverageIncome: null,
            AverageExpense: null,
            AverageNet: null,
            AverageSavings: null,
            AverageSavingsRate: null);
    }
}

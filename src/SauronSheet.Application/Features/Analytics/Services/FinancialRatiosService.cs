namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using DTOs;

/// <summary>
/// Pure service that computes financial ratios (REQ-011) from transactions.
/// No external dependencies. All ratios nullable — null means division by zero or no data.
/// </summary>
public static class FinancialRatiosService
{
    /// <summary>
    /// Computes financial ratios for the given year's transactions.
    /// </summary>
    /// <param name="transactions">All transactions for the selected year (unfiltered).</param>
    /// <param name="year">The selected year (used for days calculation).</param>
    /// <returns>AnnualDashboardRatiosDto with computed values.</returns>
    public static AnnualDashboardRatiosDto Compute(IReadOnlyList<Transaction> transactions, int year)
    {
        List<Transaction> nonZeroTransactions = transactions
            .Where(t => !t.Amount.IsZero)
            .ToList();

        int transactionCount = nonZeroTransactions.Count;

        if (transactionCount == 0)
        {
            return new AnnualDashboardRatiosDto(
                SavingsRate: null,
                AverageMonthlyIncome: null,
                AverageMonthlyExpense: null,
                AverageMonthlySavings: null,
                AverageDailyExpense: null,
                AveragePerTransaction: null,
                TransactionCount: 0,
                AverageOperationsPerMonth: null);
        }

        decimal totalIncome = nonZeroTransactions
            .Where(t => t.Amount.IsPositive)
            .Sum(t => Math.Abs(t.Amount.Amount));

        decimal totalExpense = nonZeroTransactions
            .Where(t => t.Amount.IsNegative)
            .Sum(t => Math.Abs(t.Amount.Amount));

        decimal totalNet = totalIncome - totalExpense;
        int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;

        decimal? savingsRate = totalIncome > 0m
            ? Math.Round(totalNet / totalIncome * 100m, 2)
            : null;

        decimal? averageMonthlyIncome = totalIncome > 0m
            ? Math.Round(totalIncome / 12m, 2)
            : null;

        decimal? averageMonthlyExpense = totalExpense > 0m
            ? Math.Round(totalExpense / 12m, 2)
            : null;

        decimal? averageMonthlySavings = totalNet > 0m
            ? Math.Round(totalNet / 12m, 2)
            : null;

        decimal? averageDailyExpense = totalExpense > 0m && daysInYear > 0
            ? Math.Round(totalExpense / daysInYear, 2)
            : null;

        decimal? averagePerTransaction = transactionCount > 0
            ? Math.Round((totalIncome + totalExpense) / transactionCount, 2)
            : null;

        decimal? averageOperationsPerMonth = Math.Round((decimal)transactionCount / 12m, 2);

        return new AnnualDashboardRatiosDto(
            SavingsRate: savingsRate,
            AverageMonthlyIncome: averageMonthlyIncome,
            AverageMonthlyExpense: averageMonthlyExpense,
            AverageMonthlySavings: averageMonthlySavings,
            AverageDailyExpense: averageDailyExpense,
            AveragePerTransaction: averagePerTransaction,
            TransactionCount: transactionCount,
            AverageOperationsPerMonth: averageOperationsPerMonth);
    }
}

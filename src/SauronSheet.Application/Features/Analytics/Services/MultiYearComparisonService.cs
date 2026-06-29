namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using DTOs;

/// <summary>
/// Pure service that aggregates income/expense/savings/balance by year (REQ-003).
/// Computes best/worst year, averages, and provides prev/next year pointers.
/// Returns null when only 1 year of data exists.
/// No external dependencies — receives all data as parameters.
/// </summary>
public static class MultiYearComparisonService
{
    /// <summary>
    /// Computes multi-year comparison data from already-partitioned transactions.
    /// </summary>
    /// <param name="transactionsByYear">Transactions partitioned by year (key = year).</param>
    /// <param name="selectedYear">The currently selected/highlighted year.</param>
    /// <returns>Multi-year DTO, or null if fewer than 2 years of data.</returns>
    public static AnnualDashboardMultiYearDto? Compute(
        Dictionary<int, List<Transaction>> transactionsByYear,
        int selectedYear)
    {
        if (transactionsByYear.Count < 2)
        {
            return null;
        }

        List<int> sortedYears = transactionsByYear.Keys.OrderBy(y => y).ToList();

        // Compute aggregates per year
        List<decimal> incomes = new(sortedYears.Count);
        List<decimal> expenses = new(sortedYears.Count);
        List<decimal> savings = new(sortedYears.Count);
        List<decimal> balances = new(sortedYears.Count);

        foreach (int year in sortedYears)
        {
            List<Transaction> yearTransactions = transactionsByYear[year];

            decimal yearIncome = yearTransactions
                .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
                .Sum(t => Math.Abs(t.Amount.Amount));

            decimal yearExpense = yearTransactions
                .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
                .Sum(t => Math.Abs(t.Amount.Amount));

            decimal yearSavings = yearIncome - yearExpense;

            incomes.Add(yearIncome);
            expenses.Add(yearExpense);
            savings.Add(yearSavings);
            balances.Add(yearSavings);
        }

        // Compute average across all years
        decimal avgIncome = Math.Round(incomes.Average(), 2);
        decimal avgExpense = Math.Round(expenses.Average(), 2);
        decimal avgSavings = Math.Round(savings.Average(), 2);
        decimal avgBalance = Math.Round(balances.Average(), 2);

        // Find best and worst year (by savings)
        int bestYear = sortedYears[0];
        int worstYear = sortedYears[0];
        decimal bestSavings = savings[0];
        decimal worstSavings = savings[0];

        for (int i = 1; i < sortedYears.Count; i++)
        {
            if (savings[i] > bestSavings)
            {
                bestSavings = savings[i];
                bestYear = sortedYears[i];
            }

            if (savings[i] < worstSavings)
            {
                worstSavings = savings[i];
                worstYear = sortedYears[i];
            }
        }

        // Find selected year index
        int selectedIndex = sortedYears.IndexOf(selectedYear);

        // Previous year value
        MultiYearComparisonDto? previousYearValue = null;
        if (selectedIndex > 0)
        {
            int prevIdx = selectedIndex - 1;
            previousYearValue = new MultiYearComparisonDto(
                incomes[prevIdx], expenses[prevIdx], savings[prevIdx], balances[prevIdx]);
        }

        // Next year value
        MultiYearComparisonDto? nextYearValue = null;
        if (selectedIndex >= 0 && selectedIndex < sortedYears.Count - 1)
        {
            int nextIdx = selectedIndex + 1;
            nextYearValue = new MultiYearComparisonDto(
                incomes[nextIdx], expenses[nextIdx], savings[nextIdx], balances[nextIdx]);
        }

        return new AnnualDashboardMultiYearDto(
            Years: sortedYears.AsReadOnly(),
            Incomes: incomes.AsReadOnly(),
            Expenses: expenses.AsReadOnly(),
            Savings: savings.AsReadOnly(),
            Balances: balances.AsReadOnly(),
            HighlightYear: selectedYear,
            PreviousYearValue: previousYearValue,
            NextYearValue: nextYearValue,
            Average: new MultiYearComparisonDto(avgIncome, avgExpense, avgSavings, avgBalance),
            BestYear: bestYear,
            WorstYear: worstYear);
    }
}

namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using DTOs;

/// <summary>
/// Pure service that computes monthly income/expense/savings evolution (REQ-004).
/// Provides January-December arrays plus overlay averages from previous year
/// and historical data. Detects best/worst months.
/// No external dependencies.
/// </summary>
public static class MonthlyEvolutionService
{
    /// <summary>
    /// Computes monthly evolution data for the given year.
    /// </summary>
    /// <param name="transactions">All transactions for the selected year.</param>
    /// <param name="year">The selected year.</param>
    /// <param name="allYearsTransactions">Optional — all-years partitioned data for historical overlays.</param>
    /// <returns>AnnualDashboardMonthlyDto with 12-month arrays.</returns>
    public static AnnualDashboardMonthlyDto Compute(
        IReadOnlyList<Transaction> transactions,
        int year,
        Dictionary<int, List<Transaction>>? allYearsTransactions)
    {
        decimal[] incomes = new decimal[12];
        decimal[] expenses = new decimal[12];
        decimal[] savings = new decimal[12];

        // Aggregate by month
        foreach (Transaction t in transactions)
        {
            if (t.Amount.IsZero)
                continue;

            int month = t.Date.Month - 1; // 0-based
            decimal absAmount = Math.Abs(t.Amount.Amount);

            if (t.Amount.IsPositive)
            {
                incomes[month] += absAmount;
            }
            else
            {
                expenses[month] += absAmount;
            }
        }

        // Compute monthly savings
        for (int m = 0; m < 12; m++)
        {
            savings[m] = incomes[m] - expenses[m];
        }

        // Compute previous year averages
        decimal? prevYearAvgIncome = null;
        decimal? prevYearAvgExpense = null;
        if (allYearsTransactions != null && allYearsTransactions.TryGetValue(year - 1, out List<Transaction>? prevYearTrx) && prevYearTrx.Count > 0)
        {
            decimal prevIncome = prevYearTrx
                .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
                .Sum(t => Math.Abs(t.Amount.Amount));
            decimal prevExpense = prevYearTrx
                .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
                .Sum(t => Math.Abs(t.Amount.Amount));

            prevYearAvgIncome = Math.Round(prevIncome / 12m, 2);
            prevYearAvgExpense = Math.Round(prevExpense / 12m, 2);
        }

        // Compute historical averages (all years except current)
        decimal? histAvgIncome = null;
        decimal? histAvgExpense = null;
        if (allYearsTransactions != null)
        {
            int histYearCount = 0;
            decimal histIncome = 0m;
            decimal histExpense = 0m;

            foreach (KeyValuePair<int, List<Transaction>> kvp in allYearsTransactions)
            {
                if (kvp.Key >= year)
                    continue; // Skip current and future years

                histYearCount++;
                histIncome += kvp.Value
                    .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
                    .Sum(t => Math.Abs(t.Amount.Amount));
                histExpense += kvp.Value
                    .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
                    .Sum(t => Math.Abs(t.Amount.Amount));
            }

            if (histYearCount > 0)
            {
                histAvgIncome = Math.Round(histIncome / histYearCount / 12m, 2);
                histAvgExpense = Math.Round(histExpense / histYearCount / 12m, 2);
            }
        }

        // Best/worst month detection
        int? bestIncomeMonth = null;
        int? bestExpenseMonth = null;
        int? worstIncomeMonth = null;
        int? worstExpenseMonth = null;

        (bestIncomeMonth, worstIncomeMonth) = FindBestWorstMonth(incomes, higherIsBetter: true);
        (bestExpenseMonth, worstExpenseMonth) = FindBestWorstMonth(expenses, higherIsBetter: false);

        return new AnnualDashboardMonthlyDto(
            Incomes: incomes.AsReadOnly(),
            Expenses: expenses.AsReadOnly(),
            Savings: savings.AsReadOnly(),
            PreviousYearAverageIncome: prevYearAvgIncome,
            PreviousYearAverageExpense: prevYearAvgExpense,
            HistoricalAverageIncome: histAvgIncome,
            HistoricalAverageExpense: histAvgExpense,
            BestIncomeMonth: bestIncomeMonth,
            BestExpenseMonth: bestExpenseMonth,
            WorstIncomeMonth: worstIncomeMonth,
            WorstExpenseMonth: worstExpenseMonth);
    }

    /// <summary>
    /// Finds the index of the best and worst non-zero month.
    /// For higherIsBetter=true (income): best=highest, worst=lowest.
    /// For higherIsBetter=false (expense): best=lowest, worst=highest.
    /// Returns null for both if all values are zero.
    /// </summary>
    private static (int? Best, int? Worst) FindBestWorstMonth(decimal[] monthlyValues, bool higherIsBetter)
    {
        int? best = null;
        int? worst = null;

        for (int m = 0; m < 12; m++)
        {
            if (monthlyValues[m] == 0m)
                continue;

            if (!best.HasValue)
            {
                best = m;
                worst = m;
                continue;
            }

            int currentBest = best!.Value;
            int currentWorst = worst!.Value;

            if ((higherIsBetter && monthlyValues[m] > monthlyValues[currentBest]) ||
                (!higherIsBetter && monthlyValues[m] < monthlyValues[currentBest]))
            {
                best = m;
            }

            if ((higherIsBetter && monthlyValues[m] < monthlyValues[currentWorst]) ||
                (!higherIsBetter && monthlyValues[m] > monthlyValues[currentWorst]))
            {
                worst = m;
            }
        }

        // If all non-zero months have the same value, worst is the last one
        if (best.HasValue && worst.HasValue)
        {
            int bestIdx = best.Value;
            int worstIdx = worst.Value;
            if (monthlyValues[bestIdx] == monthlyValues[worstIdx])
            {
                for (int m = 11; m >= 0; m--)
                {
                    if (monthlyValues[m] == monthlyValues[bestIdx])
                    {
                        worst = m;
                        break;
                    }
                }
            }
        }

        return (best, worst);
    }
}

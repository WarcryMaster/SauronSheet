namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using DTOs;

/// <summary>
/// Pure service that computes the Health Score (REQ-012) — 0-100 rule-based score.
/// 6 sub-scores with weights: Savings(25%), IncomeStab(15%), ExpenseStab(15%),
/// CatDep(10%), Balance(20%), Trend(15%).
/// </summary>
public static class HealthScoreService
{
    // Weights from spec REQ-012
    private const decimal SavingsWeight = 0.25m;
    private const decimal IncomeStabilityWeight = 0.15m;
    private const decimal ExpenseStabilityWeight = 0.15m;
    private const decimal CategoryDependencyWeight = 0.10m;
    private const decimal BalanceWeight = 0.20m;
    private const decimal TrendWeight = 0.15m;

    /// <summary>
    /// Computes the health score and all sub-scores.
    /// Returns all-null scores when no transactions exist.
    /// </summary>
    public static AnnualDashboardHealthScoreDto Compute(
        IReadOnlyList<Transaction> transactions,
        AnnualDashboardSummaryDto summary,
        AnnualDashboardRatiosDto ratios,
        IReadOnlyList<AnnualAnalysisRowDto> classifiedRows)
    {
        if (transactions.Count == 0)
        {
            return new AnnualDashboardHealthScoreDto(
                Total: null,
                SavingsScore: null,
                IncomeStabilityScore: null,
                ExpenseStabilityScore: null,
                CategoryDependencyScore: null,
                BalanceScore: null,
                TrendScore: null,
                SavingsWeight: SavingsWeight,
                IncomeStabilityWeight: IncomeStabilityWeight,
                ExpenseStabilityWeight: ExpenseStabilityWeight,
                CategoryDependencyWeight: CategoryDependencyWeight,
                BalanceWeight: BalanceWeight,
                TrendWeight: TrendWeight);
        }

        // 1. Savings Score (25%): min(savingsRate / 20% * 100, 100)
        decimal savingsScore = ratios.SavingsRate.HasValue
            ? Math.Min(ratios.SavingsRate.Value / 20m * 100m, 100m)
            : 0m;
        savingsScore = Math.Round(Math.Max(savingsScore, 0m), 2);

        // 2. Income Stability Score (15%): 100 - min(CV * 100, 100)
        decimal incomeCv = ComputeCoefficientOfVariation(transactions
            .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
            .GroupBy(t => t.Date.Month)
            .Select(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
            .ToList());
        decimal incomeStabilityScore = Math.Round(Math.Max(100m - Math.Min(incomeCv * 100m, 100m), 0m), 2);

        // 3. Expense Stability Score (15%): 100 - min(CV * 100, 100)
        decimal expenseCv = ComputeCoefficientOfVariation(transactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .GroupBy(t => t.Date.Month)
            .Select(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
            .ToList());
        decimal expenseStabilityScore = Math.Round(Math.Max(100m - Math.Min(expenseCv * 100m, 100m), 0m), 2);

        // 4. Category Dependency Score (10%): 100 - top3Share
        decimal top3Share = ComputeTopCategoryShare(classifiedRows);
        decimal categoryDependencyScore = Math.Round(Math.Max(100m - top3Share, 0m), 2);

        // 5. Balance Score (20%): min(I/E ratio * 50, 100)
        decimal balanceScore = summary.Expense > 0m
            ? Math.Round(Math.Min(summary.Income / summary.Expense * 50m, 100m), 2)
            : summary.Income > 0m ? 100m : 0m;

        // 6. Trend Score (15%): based on income and savings YoY direction
        decimal trendScore = ComputeTrendScore(summary);

        // Weighted total
        decimal total = Math.Round(
            savingsScore * SavingsWeight +
            incomeStabilityScore * IncomeStabilityWeight +
            expenseStabilityScore * ExpenseStabilityWeight +
            categoryDependencyScore * CategoryDependencyWeight +
            balanceScore * BalanceWeight +
            trendScore * TrendWeight, 2);

        return new AnnualDashboardHealthScoreDto(
            Total: total,
            SavingsScore: savingsScore,
            IncomeStabilityScore: incomeStabilityScore,
            ExpenseStabilityScore: expenseStabilityScore,
            CategoryDependencyScore: categoryDependencyScore,
            BalanceScore: balanceScore,
            TrendScore: trendScore,
            SavingsWeight: SavingsWeight,
            IncomeStabilityWeight: IncomeStabilityWeight,
            ExpenseStabilityWeight: ExpenseStabilityWeight,
            CategoryDependencyWeight: CategoryDependencyWeight,
            BalanceWeight: BalanceWeight,
            TrendWeight: TrendWeight);
    }

    /// <summary>
    /// Computes coefficient of variation (σ/μ) from a list of monthly values.
    /// Returns 0 for empty or single-valued lists (no variance).
    /// </summary>
    private static decimal ComputeCoefficientOfVariation(List<decimal> monthlyValues)
    {
        if (monthlyValues.Count <= 1)
            return 0m;

        decimal mean = monthlyValues.Average();
        if (mean == 0m)
            return 0m;

        double variance = monthlyValues
            .Select(v => (double)(v - mean) * (double)(v - mean))
            .Average();

        double stdDev = Math.Sqrt(variance);
        return (decimal)(stdDev / (double)mean);
    }

    /// <summary>
    /// Computes the percentage share of the top 3 categories from classified rows.
    /// </summary>
    private static decimal ComputeTopCategoryShare(IReadOnlyList<AnnualAnalysisRowDto> classifiedRows)
    {
        List<decimal> expenseAmounts = classifiedRows
            .Where(r => !r.IsIncome)
            .Select(r => r.MonthlyAmounts.Sum())
            .OrderByDescending(a => a)
            .ToList();

        if (expenseAmounts.Count == 0)
            return 0m;

        decimal totalExpense = expenseAmounts.Sum();
        if (totalExpense == 0m)
            return 0m;

        decimal top3Sum = expenseAmounts.Take(3).Sum();
        return Math.Round(top3Sum / totalExpense * 100m, 2);
    }

    /// <summary>
    /// Computes trend score based on income and savings direction.
    /// </summary>
    private static decimal ComputeTrendScore(AnnualDashboardSummaryDto summary)
    {
        if (!summary.IncomeChangePct.HasValue || !summary.SavingsChangePct.HasValue)
            return 50m; // Neutral — no previous data

        bool incomeUp = summary.IncomeChangePct.Value > 0m;
        bool savingsUp = summary.SavingsChangePct.Value > 0m;
        bool expenseDown = summary.ExpenseChangePct.HasValue && summary.ExpenseChangePct.Value < 0m;

        int positiveSignals = 0;
        if (incomeUp) positiveSignals++;
        if (savingsUp) positiveSignals++;
        if (expenseDown) positiveSignals++;

        return positiveSignals switch
        {
            3 => 100m,
            2 => 75m,
            1 => 40m,
            _ => 0m
        };
    }
}

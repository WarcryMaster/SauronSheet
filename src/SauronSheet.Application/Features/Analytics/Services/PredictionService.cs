namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using DTOs;

/// <summary>
/// Deterministic prediction service (REQ-016) using simple linear regression.
/// No AI models.
/// </summary>
public static class PredictionService
{
    private const int MinimumYears = 2;

    public static PredictionDto Compute(
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> yearlySummaries,
        int selectedYear)
    {
        List<KeyValuePair<int, AnnualDashboardSummaryDto>> sorted = yearlySummaries
            .Where(x => x.Key <= selectedYear)
            .OrderBy(x => x.Key)
            .ToList();

        if (sorted.Count < MinimumYears)
        {
            return new PredictionDto(
                ProjectedIncome: null,
                ProjectedExpense: null,
                ProjectedSavings: null,
                ProjectedBalance: null,
                Confidence: null,
                YearsRequired: MinimumYears,
                HasEnoughData: false,
                Message: "2 years needed");
        }

        List<double> x = Enumerable.Range(0, sorted.Count).Select(i => (double)i).ToList();
        List<double> income = sorted.Select(s => (double)s.Value.Income).ToList();
        List<double> expense = sorted.Select(s => (double)s.Value.Expense).ToList();
        List<double> savings = sorted.Select(s => (double)s.Value.Savings).ToList();
        List<double> balance = sorted.Select(s => (double)s.Value.Net).ToList();

        double nextX = sorted.Count;

        (double IncomeProjected, double IncomeR2) incomeProjection = PredictAndScore(x, income, nextX);
        (double ExpenseProjected, double ExpenseR2) expenseProjection = PredictAndScore(x, expense, nextX);
        (double SavingsProjected, double SavingsR2) savingsProjection = PredictAndScore(x, savings, nextX);
        (double BalanceProjected, double BalanceR2) balanceProjection = PredictAndScore(x, balance, nextX);

        decimal confidence = decimal.Round((decimal)(
            (incomeProjection.IncomeR2 + expenseProjection.ExpenseR2 + savingsProjection.SavingsR2 + balanceProjection.BalanceR2) / 4.0), 4);

        return new PredictionDto(
            ProjectedIncome: decimal.Round((decimal)incomeProjection.IncomeProjected, 2),
            ProjectedExpense: decimal.Round((decimal)expenseProjection.ExpenseProjected, 2),
            ProjectedSavings: decimal.Round((decimal)savingsProjection.SavingsProjected, 2),
            ProjectedBalance: decimal.Round((decimal)balanceProjection.BalanceProjected, 2),
            Confidence: ClampConfidence(confidence),
            YearsRequired: MinimumYears,
            HasEnoughData: true,
            Message: "Projection generated from linear regression.");
    }

    private static (double Predicted, double R2) PredictAndScore(IReadOnlyList<double> x, IReadOnlyList<double> y, double xToPredict)
    {
        (double slope, double intercept) regression = FitLinearRegression(x, y);
        double predicted = regression.slope * xToPredict + regression.intercept;
        double r2 = ComputeR2(x, y, regression.slope, regression.intercept);
        return (predicted, r2);
    }

    private static (double slope, double intercept) FitLinearRegression(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = x.Count;
        double xMean = x.Average();
        double yMean = y.Average();

        double numerator = 0d;
        double denominator = 0d;
        for (int i = 0; i < n; i++)
        {
            double xDelta = x[i] - xMean;
            numerator += xDelta * (y[i] - yMean);
            denominator += xDelta * xDelta;
        }

        if (Math.Abs(denominator) < double.Epsilon)
        {
            return (0d, yMean);
        }

        double slope = numerator / denominator;
        double intercept = yMean - (slope * xMean);
        return (slope, intercept);
    }

    private static double ComputeR2(IReadOnlyList<double> x, IReadOnlyList<double> y, double slope, double intercept)
    {
        double yMean = y.Average();
        double ssTot = y.Sum(v => Math.Pow(v - yMean, 2));
        if (Math.Abs(ssTot) < double.Epsilon)
        {
            return 1d;
        }

        double ssRes = 0d;
        for (int i = 0; i < x.Count; i++)
        {
            double predicted = (slope * x[i]) + intercept;
            ssRes += Math.Pow(y[i] - predicted, 2);
        }

        double r2 = 1d - (ssRes / ssTot);
        return Math.Clamp(r2, 0d, 1d);
    }

    private static decimal ClampConfidence(decimal confidence)
    {
        if (confidence < 0m)
        {
            return 0m;
        }

        if (confidence > 1m)
        {
            return 1m;
        }

        return confidence;
    }
}

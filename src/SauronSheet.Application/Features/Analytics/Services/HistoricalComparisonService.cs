namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using DTOs;

/// <summary>
/// Pure historical comparison service (REQ-017).
/// </summary>
public static class HistoricalComparisonService
{
    public static HistoricalComparisonDto Compute(
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> yearlySummaries,
        int selectedYear)
    {
        if (!yearlySummaries.TryGetValue(selectedYear, out AnnualDashboardSummaryDto? current)
            || yearlySummaries.Count < 2)
        {
            return new HistoricalComparisonDto(
                Income: null,
                Expense: null,
                Savings: null,
                SavingsRate: null,
                Balance: null,
                Message: "Need 2+ years of data");
        }

        List<AnnualDashboardSummaryDto> all = yearlySummaries.Values.OrderBy(v => v.Year).ToList();
        AnnualDashboardSummaryDto? previous = all.Where(v => v.Year < selectedYear).OrderByDescending(v => v.Year).FirstOrDefault();

        return new HistoricalComparisonDto(
            Income: BuildMetric(current.Income, previous?.Income, all.Select(v => v.Income).ToList()),
            Expense: BuildMetric(current.Expense, previous?.Expense, all.Select(v => v.Expense).ToList()),
            Savings: BuildMetric(current.Savings, previous?.Savings, all.Select(v => v.Savings).ToList()),
            SavingsRate: BuildMetric(current.SavingsRate, previous?.SavingsRate, all.Select(v => v.SavingsRate).ToList()),
            Balance: BuildMetric(current.Net, previous?.Net, all.Select(v => v.Net).ToList()),
            Message: null);
    }

    private static HistoricalComparisonMetricDto BuildMetric(
        decimal current,
        decimal? previous,
        IReadOnlyList<decimal> series)
    {
        decimal average = decimal.Round(series.Average(), 2);
        decimal best = series.Max();
        decimal worst = series.Min();

        return new HistoricalComparisonMetricDto(
            Current: current,
            Previous: previous,
            PreviousDiffAbs: previous.HasValue ? decimal.Round(current - previous.Value, 2) : null,
            PreviousDiffPct: previous.HasValue ? ComputePct(current, previous.Value) : null,
            Average: average,
            AverageDiffAbs: decimal.Round(current - average, 2),
            AverageDiffPct: ComputePct(current, average),
            Best: best,
            BestDiffAbs: decimal.Round(current - best, 2),
            BestDiffPct: ComputePct(current, best),
            Worst: worst,
            WorstDiffAbs: decimal.Round(current - worst, 2),
            WorstDiffPct: ComputePct(current, worst));
    }

    private static decimal? ComputePct(decimal current, decimal reference)
    {
        if (reference == 0m)
        {
            return null;
        }

        return decimal.Round((current - reference) / Math.Abs(reference) * 100m, 2);
    }
}

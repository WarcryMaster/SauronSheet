namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;

[Trait("Category", "Application")]
public class HistoricalComparisonServiceTests
{
    [Fact]
    public void Compute_WithTwoOrMoreYears_ReturnsComparisonMetrics()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        HistoricalComparisonDto comparison = HistoricalComparisonService.Compute(summaries, 2026);

        Assert.Null(comparison.Message);
        Assert.NotNull(comparison.Income);
        Assert.NotNull(comparison.Expense);
        Assert.NotNull(comparison.Savings);
        Assert.NotNull(comparison.Balance);
    }

    [Fact]
    public void Compute_WithSingleYear_ReturnsNeedTwoPlusMessage()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = new Dictionary<int, AnnualDashboardSummaryDto>
        {
            [2026] = CreateSummary(2026, 300m, 100m)
        };

        HistoricalComparisonDto comparison = HistoricalComparisonService.Compute(summaries, 2026);

        Assert.NotNull(comparison.Message);
        Assert.Contains("Need 2+", comparison.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_ProducesAbsoluteDiffValues()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        HistoricalComparisonDto comparison = HistoricalComparisonService.Compute(summaries, 2026);

        Assert.NotNull(comparison.Income);
        Assert.Equal(50m, comparison.Income!.PreviousDiffAbs);
    }

    [Fact]
    public void Compute_ProducesPercentDiffValues()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        HistoricalComparisonDto comparison = HistoricalComparisonService.Compute(summaries, 2026);

        Assert.NotNull(comparison.Income);
        Assert.NotNull(comparison.Income!.PreviousDiffPct);
        Assert.True(comparison.Income.PreviousDiffPct > 0m);
    }

    private static IReadOnlyDictionary<int, AnnualDashboardSummaryDto> BuildSummaries()
    {
        return new Dictionary<int, AnnualDashboardSummaryDto>
        {
            [2024] = CreateSummary(2024, 200m, 120m),
            [2025] = CreateSummary(2025, 250m, 130m),
            [2026] = CreateSummary(2026, 300m, 140m),
        };
    }

    private static AnnualDashboardSummaryDto CreateSummary(int year, decimal income, decimal expense)
    {
        decimal net = income - expense;
        decimal savingsRate = income <= 0m ? 0m : decimal.Round(net / income * 100m, 2);

        return new AnnualDashboardSummaryDto(
            Income: income,
            Expense: expense,
            Net: net,
            Savings: net,
            SavingsRate: savingsRate,
            Year: year,
            HasPreviousYear: false,
            HasNextYear: false,
            YearRank: null,
            TotalYears: 0,
            PreviousYearIncome: null,
            PreviousYearExpense: null,
            PreviousYearNet: null,
            PreviousYearSavings: null,
            PreviousYearSavingsRate: null,
            IncomeChangeAbs: null,
            IncomeChangePct: null,
            ExpenseChangeAbs: null,
            ExpenseChangePct: null,
            NetChangeAbs: null,
            NetChangePct: null,
            SavingsChangeAbs: null,
            SavingsChangePct: null,
            AverageIncome: null,
            AverageExpense: null,
            AverageNet: null,
            AverageSavings: null,
            AverageSavingsRate: null);
    }
}

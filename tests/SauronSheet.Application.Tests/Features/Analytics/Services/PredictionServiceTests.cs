namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;

[Trait("Category", "Application")]
public class PredictionServiceTests
{
    [Fact]
    public void Compute_WithTwoOrMoreYears_ReturnsLinearProjection()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = new Dictionary<int, AnnualDashboardSummaryDto>
        {
            [2024] = CreateSummary(2024, 100m, 40m),
            [2025] = CreateSummary(2025, 200m, 80m),
            [2026] = CreateSummary(2026, 300m, 120m),
        };

        PredictionDto prediction = PredictionService.Compute(summaries, 2026);

        Assert.True(prediction.HasEnoughData);
        Assert.NotNull(prediction.ProjectedIncome);
        Assert.Equal(400m, prediction.ProjectedIncome);
    }

    [Fact]
    public void Compute_WithSingleYear_ReturnsTwoYearsNeededMessage()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = new Dictionary<int, AnnualDashboardSummaryDto>
        {
            [2026] = CreateSummary(2026, 300m, 120m),
        };

        PredictionDto prediction = PredictionService.Compute(summaries, 2026);

        Assert.False(prediction.HasEnoughData);
        Assert.Contains("2 years needed", prediction.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_Confidence_IsBoundedByZeroAndOne()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = new Dictionary<int, AnnualDashboardSummaryDto>
        {
            [2023] = CreateSummary(2023, 300m, 200m),
            [2024] = CreateSummary(2024, 280m, 170m),
            [2025] = CreateSummary(2025, 350m, 230m),
            [2026] = CreateSummary(2026, 330m, 210m),
        };

        PredictionDto prediction = PredictionService.Compute(summaries, 2026);

        Assert.NotNull(prediction.Confidence);
        Assert.InRange(prediction.Confidence!.Value, 0m, 1m);
    }

    [Fact]
    public void Compute_WithExtremeValues_ReturnsFinitePredictions()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = new Dictionary<int, AnnualDashboardSummaryDto>
        {
            [2024] = CreateSummary(2024, 1_000_000m, 500_000m),
            [2025] = CreateSummary(2025, 1_200_000m, 600_000m),
            [2026] = CreateSummary(2026, 1_400_000m, 700_000m),
        };

        PredictionDto prediction = PredictionService.Compute(summaries, 2026);

        Assert.True(prediction.HasEnoughData);
        Assert.NotNull(prediction.ProjectedIncome);
        Assert.NotNull(prediction.ProjectedExpense);
        Assert.NotNull(prediction.ProjectedSavings);
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

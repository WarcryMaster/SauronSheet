namespace SauronSheet.Application.Tests.Features.Analytics.DTOs;

using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Tests for T3 DTOs (PR 3).
/// </summary>
[Trait("Category", "Application")]
public class AnnualDashboardT3DtosTests
{
    [Fact]
    public void AnomalyDto_ConstructedWithValues_PropertiesMatch()
    {
        AnomalyDto dto = new(
            Category: "Food",
            Month: 8,
            Amount: 450m,
            Mean: 120m,
            StandardDeviation: 80m,
            Type: "anomaly",
            Description: "August expense exceeded statistical threshold.");

        Assert.Equal("Food", dto.Category);
        Assert.Equal(8, dto.Month);
        Assert.Equal(450m, dto.Amount);
        Assert.Equal(120m, dto.Mean);
        Assert.Equal(80m, dto.StandardDeviation);
        Assert.Equal("anomaly", dto.Type);
    }

    [Fact]
    public void DiscoveryDto_ConstructedWithValues_PropertiesMatch()
    {
        DiscoveryDto dto = new(
            Icon: "💡",
            Title: "Top 2 categories dominate spending",
            Description: "56% of yearly expenses come from two categories.",
            Category: "category-concentration");

        Assert.Equal("💡", dto.Icon);
        Assert.Equal("Top 2 categories dominate spending", dto.Title);
        Assert.Equal("56% of yearly expenses come from two categories.", dto.Description);
        Assert.Equal("category-concentration", dto.Category);
    }

    [Fact]
    public void AchievementDto_ConstructedWithValues_PropertiesMatch()
    {
        AchievementDto dto = new(
            Id: "best-year",
            Title: "Best Year",
            Description: "This year has the highest net savings.",
            Icon: "🏆",
            Unlocked: true);

        Assert.Equal("best-year", dto.Id);
        Assert.Equal("Best Year", dto.Title);
        Assert.Equal("This year has the highest net savings.", dto.Description);
        Assert.Equal("🏆", dto.Icon);
        Assert.True(dto.Unlocked);
    }

    [Fact]
    public void TrendDto_ConstructedWithValues_PropertiesMatch()
    {
        TrendDto dto = new(
            Category: "Restaurants",
            Direction: "declining",
            ChangePercentage: -12.5m,
            Icon: "↓");

        Assert.Equal("Restaurants", dto.Category);
        Assert.Equal("declining", dto.Direction);
        Assert.Equal(-12.5m, dto.ChangePercentage);
        Assert.Equal("↓", dto.Icon);
    }

    [Fact]
    public void PredictionDto_ConstructedWithValues_PropertiesMatch()
    {
        PredictionDto dto = new(
            ProjectedIncome: 52000m,
            ProjectedExpense: 31000m,
            ProjectedSavings: 21000m,
            ProjectedBalance: 21000m,
            Confidence: 0.84m,
            YearsRequired: 2,
            HasEnoughData: true,
            Message: "Projection generated from linear regression.");

        Assert.Equal(52000m, dto.ProjectedIncome);
        Assert.Equal(31000m, dto.ProjectedExpense);
        Assert.Equal(21000m, dto.ProjectedSavings);
        Assert.Equal(21000m, dto.ProjectedBalance);
        Assert.Equal(0.84m, dto.Confidence);
        Assert.Equal(2, dto.YearsRequired);
        Assert.True(dto.HasEnoughData);
    }

    [Fact]
    public void HistoricalComparisonDto_ConstructedWithValues_PropertiesMatch()
    {
        HistoricalComparisonMetricDto income = new(
            Current: 50000m,
            Previous: 47000m,
            PreviousDiffAbs: 3000m,
            PreviousDiffPct: 6.38m,
            Average: 44000m,
            AverageDiffAbs: 6000m,
            AverageDiffPct: 13.64m,
            Best: 52000m,
            BestDiffAbs: -2000m,
            BestDiffPct: -3.85m,
            Worst: 39000m,
            WorstDiffAbs: 11000m,
            WorstDiffPct: 28.21m);

        HistoricalComparisonDto dto = new(
            Income: income,
            Expense: income,
            Savings: income,
            SavingsRate: income,
            Balance: income,
            Message: null);

        Assert.NotNull(dto.Income);
        Assert.NotNull(dto.Balance);
        Assert.Equal(50000m, dto.Income!.Current);
        Assert.Equal(3000m, dto.Income.PreviousDiffAbs);
        Assert.Equal(13.64m, dto.Income.AverageDiffPct);
        Assert.Equal(28.21m, dto.Balance!.WorstDiffPct);
        Assert.Null(dto.Message);
    }
}

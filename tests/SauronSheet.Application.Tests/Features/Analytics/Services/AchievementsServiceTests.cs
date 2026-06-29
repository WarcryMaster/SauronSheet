namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;

[Trait("Category", "Application")]
public class AchievementsServiceTests
{
    [Fact]
    public void Compute_WhenCurrentYearBestNet_UnlocksBestYear()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        IReadOnlyList<AchievementDto> achievements = AchievementsService.Compute(
            yearlySummaries: summaries,
            selectedYear: 2026,
            yearlyRestaurantExpenses: new Dictionary<int, decimal>());

        Assert.Contains(achievements, a => a.Id == "best-year" && a.Unlocked);
    }

    [Fact]
    public void Compute_WhenCurrentYearHasMaxSavings_UnlocksSavingsRecord()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        IReadOnlyList<AchievementDto> achievements = AchievementsService.Compute(
            yearlySummaries: summaries,
            selectedYear: 2026,
            yearlyRestaurantExpenses: new Dictionary<int, decimal>());

        Assert.Contains(achievements, a => a.Id == "savings-record" && a.Unlocked);
    }

    [Fact]
    public void Compute_WhenCurrentYearHasMaxIncome_UnlocksIncomeRecord()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        IReadOnlyList<AchievementDto> achievements = AchievementsService.Compute(
            yearlySummaries: summaries,
            selectedYear: 2026,
            yearlyRestaurantExpenses: new Dictionary<int, decimal>());

        Assert.Contains(achievements, a => a.Id == "income-record" && a.Unlocked);
    }

    [Fact]
    public void Compute_WhenSavingsIncreaseThreeYears_UnlocksThreeYearStreak()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        IReadOnlyList<AchievementDto> achievements = AchievementsService.Compute(
            yearlySummaries: summaries,
            selectedYear: 2026,
            yearlyRestaurantExpenses: new Dictionary<int, decimal>());

        Assert.Contains(achievements, a => a.Id == "three-year-savings-streak" && a.Unlocked);
    }

    [Fact]
    public void Compute_WhenSelectedYearHasLowestRestaurantExpense_UnlocksLowestRestaurant()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();
        IReadOnlyDictionary<int, decimal> restaurantByYear = new Dictionary<int, decimal>
        {
            [2024] = 900m,
            [2025] = 750m,
            [2026] = 600m,
        };

        IReadOnlyList<AchievementDto> achievements = AchievementsService.Compute(
            yearlySummaries: summaries,
            selectedYear: 2026,
            yearlyRestaurantExpenses: restaurantByYear);

        Assert.Contains(achievements, a => a.Id == "lowest-restaurant" && a.Unlocked);
    }

    [Fact]
    public void Compute_WhenNetIsPositive_UnlocksZeroDebtYear()
    {
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> summaries = BuildSummaries();

        IReadOnlyList<AchievementDto> achievements = AchievementsService.Compute(
            yearlySummaries: summaries,
            selectedYear: 2026,
            yearlyRestaurantExpenses: new Dictionary<int, decimal>());

        Assert.Contains(achievements, a => a.Id == "zero-debt-year" && a.Unlocked);
    }

    private static IReadOnlyDictionary<int, AnnualDashboardSummaryDto> BuildSummaries()
    {
        return new Dictionary<int, AnnualDashboardSummaryDto>
        {
            [2024] = CreateSummary(2024, income: 40000m, expense: 32000m),
            [2025] = CreateSummary(2025, income: 42000m, expense: 31500m),
            [2026] = CreateSummary(2026, income: 45000m, expense: 30000m),
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
            TotalYears: 3,
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

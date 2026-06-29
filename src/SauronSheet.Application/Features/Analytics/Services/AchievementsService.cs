namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using DTOs;

/// <summary>
/// Pure achievements service (REQ-014).
/// </summary>
public static class AchievementsService
{
    public static IReadOnlyList<AchievementDto> Compute(
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> yearlySummaries,
        int selectedYear,
        IReadOnlyDictionary<int, decimal> yearlyRestaurantExpenses)
    {
        if (!yearlySummaries.TryGetValue(selectedYear, out AnnualDashboardSummaryDto? selected))
        {
            return Array.Empty<AchievementDto>();
        }

        decimal maxNet = yearlySummaries.Values.Max(x => x.Net);
        decimal maxSavings = yearlySummaries.Values.Max(x => x.Savings);
        decimal maxIncome = yearlySummaries.Values.Max(x => x.Income);

        bool hasThreeYearSavingsStreak = HasIncreasingSavingsStreak(yearlySummaries, selectedYear, 3);

        bool hasLowestRestaurant = false;
        if (yearlyRestaurantExpenses.Count > 0 && yearlyRestaurantExpenses.TryGetValue(selectedYear, out decimal selectedRestaurant))
        {
            decimal minimumRestaurant = yearlyRestaurantExpenses.Values.Min();
            hasLowestRestaurant = selectedRestaurant == minimumRestaurant;
        }

        List<AchievementDto> achievements = new()
        {
            new(
                Id: "best-year",
                Title: "Best Year",
                Description: "Highest net result among available years.",
                Icon: "🏆",
                Unlocked: selected.Net == maxNet),
            new(
                Id: "savings-record",
                Title: "Savings Record",
                Description: "Highest yearly savings achieved.",
                Icon: "💰",
                Unlocked: selected.Savings == maxSavings),
            new(
                Id: "income-record",
                Title: "Income Record",
                Description: "Highest yearly income achieved.",
                Icon: "📈",
                Unlocked: selected.Income == maxIncome),
            new(
                Id: "three-year-savings-streak",
                Title: "3-Year Savings Streak",
                Description: "Savings increased for three consecutive years.",
                Icon: "🔥",
                Unlocked: hasThreeYearSavingsStreak),
            new(
                Id: "lowest-restaurant",
                Title: "Lowest Restaurant Spending",
                Description: "Lowest restaurant spending across available years.",
                Icon: "🍽️",
                Unlocked: hasLowestRestaurant),
            new(
                Id: "zero-debt-year",
                Title: "Zero-Debt Year",
                Description: "Closed the year with positive net balance.",
                Icon: "✅",
                Unlocked: selected.Net > 0m),
        };

        return achievements.AsReadOnly();
    }

    private static bool HasIncreasingSavingsStreak(
        IReadOnlyDictionary<int, AnnualDashboardSummaryDto> yearlySummaries,
        int selectedYear,
        int years)
    {
        List<int> sortedYears = yearlySummaries.Keys
            .Where(y => y <= selectedYear)
            .OrderBy(y => y)
            .ToList();

        if (sortedYears.Count < years)
        {
            return false;
        }

        List<int> lastWindow = sortedYears.Skip(sortedYears.Count - years).Take(years).ToList();
        if (!lastWindow.Contains(selectedYear))
        {
            return false;
        }

        for (int i = 1; i < lastWindow.Count; i++)
        {
            AnnualDashboardSummaryDto previous = yearlySummaries[lastWindow[i - 1]];
            AnnualDashboardSummaryDto current = yearlySummaries[lastWindow[i]];
            if (current.Savings <= previous.Savings)
            {
                return false;
            }
        }

        return true;
    }
}

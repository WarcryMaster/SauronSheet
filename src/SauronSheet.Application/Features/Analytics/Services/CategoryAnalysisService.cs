namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using DTOs;

/// <summary>
/// Pure service that analyzes category distribution from classified rows (REQ-005, REQ-006, REQ-007).
/// Groups expense categories, computes rankings, YoY changes, and comparison table.
/// Income categories are excluded from category analysis (focus on spending).
/// No external dependencies.
/// </summary>
public static class CategoryAnalysisService
{
    /// <summary>
    /// Computes category distribution and comparison data.
    /// </summary>
    /// <param name="currentYearRows">Classified rows for the selected year (expense categories analyzed).</param>
    /// <param name="previousYearRows">Classified rows for the previous year (null if none).</param>
    /// <param name="nextYearRows">Classified rows for the next year (null if none).</param>
    /// <returns>Tuple of category items (ranked) and optional comparison table.</returns>
    public static (IReadOnlyList<CategoryItemDto> Categories, CategoryComparisonTableDto? ComparisonTable) ComputeCategories(
        IReadOnlyList<AnnualAnalysisRowDto> currentYearRows,
        IReadOnlyList<AnnualAnalysisRowDto>? previousYearRows,
        IReadOnlyList<AnnualAnalysisRowDto>? nextYearRows)
    {
        // Get expense-only rows
        List<AnnualAnalysisRowDto> expenseRows = currentYearRows
            .Where(r => !r.IsIncome)
            .OrderByDescending(r => r.MonthlyAmounts.Sum())
            .ToList();

        if (expenseRows.Count == 0)
        {
            return (Array.Empty<CategoryItemDto>(), null);
        }

        decimal totalExpense = expenseRows.Sum(r => r.MonthlyAmounts.Sum());

        // Build category lookup from previous year
        bool hasPrevYearData = previousYearRows != null && previousYearRows.Any(r => !r.IsIncome);
        Dictionary<string, decimal>? prevCategoryAmounts = null;
        if (hasPrevYearData)
        {
            prevCategoryAmounts = previousYearRows!
                .Where(r => !r.IsIncome)
                .ToDictionary(r => r.Movement, r => r.MonthlyAmounts.Sum());
        }

        // Build category lookup from next year
        Dictionary<string, decimal>? nextCategoryAmounts = null;
        if (nextYearRows != null)
        {
            nextCategoryAmounts = nextYearRows
                .Where(r => !r.IsIncome)
                .ToDictionary(r => r.Movement, r => r.MonthlyAmounts.Sum());
        }

        List<CategoryItemDto> categories = new();
        int rank = 1;

        foreach (AnnualAnalysisRowDto row in expenseRows)
        {
            decimal amount = row.MonthlyAmounts.Sum();
            decimal percentage = totalExpense > 0m
                ? Math.Round(amount / totalExpense * 100m, 0)
                : 0m;

            // YoY computation
            bool isNewThisYear = false;
            decimal? yoyAbs = null;
            decimal? yoyPct = null;
            string trend = "stable";

            if (prevCategoryAmounts != null && prevCategoryAmounts.TryGetValue(row.Movement, out decimal prevAmount))
            {
                yoyAbs = Math.Round(amount - prevAmount, 2);
                yoyPct = prevAmount != 0m
                    ? Math.Round((amount - prevAmount) / Math.Abs(prevAmount) * 100m, 2)
                    : null;

                trend = yoyPct.HasValue
                    ? (yoyPct.Value > 0m ? "up" : yoyPct.Value < 0m ? "down" : "stable")
                    : "stable";
            }
            else if (hasPrevYearData)
            {
                // Category exists in current year but NOT in previous year = new this year
                isNewThisYear = true;
                trend = "new";
            }
            // else: no prev year data at all — we can't determine if it's new

            categories.Add(new CategoryItemDto(
                CategoryName: row.Movement,
                Amount: amount,
                Percentage: percentage,
                Rank: rank,
                YoYChangeAbs: yoyAbs,
                YoYChangePct: yoyPct,
                Trend: trend,
                IsNewThisYear: isNewThisYear));

            rank++;
        }

        // Build comparison table (sorted by absolute diff descending)
        CategoryComparisonTableDto? comparisonTable = null;
        if (hasPrevYearData)
        {
            List<CategoryComparisonRowDto> tableRows = new();

            foreach (CategoryItemDto cat in categories)
            {
                if (cat.IsNewThisYear)
                    continue; // Skip new categories in comparison table

                decimal? nextAmount = nextCategoryAmounts != null && nextCategoryAmounts.TryGetValue(cat.CategoryName, out decimal na)
                    ? na
                    : null;

                decimal prevAmount = prevCategoryAmounts!.TryGetValue(cat.CategoryName, out decimal pa) ? pa : 0m;
                tableRows.Add(new CategoryComparisonRowDto(
                    CategoryName: cat.CategoryName,
                    PreviousYearAmount: prevAmount,
                    SelectedYearAmount: cat.Amount,
                    NextYearAmount: nextAmount,
                    DiffAbs: cat.YoYChangeAbs ?? 0m,
                    DiffPct: cat.YoYChangePct ?? 0m,
                    Trend: cat.Trend));
            }

            // Sort by absolute diff descending
            tableRows = tableRows
                .OrderByDescending(r => Math.Abs(r.DiffAbs))
                .ToList();

            comparisonTable = new CategoryComparisonTableDto(tableRows.AsReadOnly());
        }

        return (categories.AsReadOnly(), comparisonTable);
    }

    /// <summary>
    /// Extracts category YoY changes from computed category items.
    /// </summary>
    public static IReadOnlyDictionary<string, decimal?> BuildCategoryYoYChanges(IReadOnlyList<CategoryItemDto> categories)
    {
        Dictionary<string, decimal?> changes = categories
            .ToDictionary(c => c.CategoryName, c => c.YoYChangePct, StringComparer.OrdinalIgnoreCase);

        return changes;
    }
}

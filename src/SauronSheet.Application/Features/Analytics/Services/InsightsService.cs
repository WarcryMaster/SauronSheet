namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;
using Domain.Entities;
using DTOs;
using SauronSheet.Application.Resources;

/// <summary>
/// Pure service that generates rule-based Smart Summary (REQ-002) and insights.
/// No external dependencies, no AI — purely deterministic rules.
/// </summary>
public class InsightsService
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    public InsightsService(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// Generates a 2-4 sentence smart summary from the annual data.
    /// Returns "Sin datos para este año" when no transactions exist.
    /// </summary>
    public string GenerateSmartSummary(
        IReadOnlyList<Transaction> transactions,
        AnnualDashboardSummaryDto summary,
        AnnualDashboardRatiosDto ratios,
        IReadOnlyList<AnnualAnalysisRowDto> classifiedRows)
    {
        if (transactions.Count == 0 || summary.Income == 0m && summary.Expense == 0m)
        {
            return _localizer["Insights.EmptyYear"];
        }

        List<string> sentences = new List<string>();
        bool isSpanish = string.Equals(
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            "es",
            StringComparison.OrdinalIgnoreCase);

        // Sentence 1: Income change
        if (summary.HasPreviousYear && summary.IncomeChangePct.HasValue)
        {
            string direction = summary.IncomeChangePct.Value >= 0m
                ? (isSpanish ? "crecieron" : "increased")
                : (isSpanish ? "disminuyeron" : "decreased");
            string absChange = Math.Abs(summary.IncomeChangePct.Value).ToString("F1", CultureInfo.InvariantCulture);
            sentences.Add(isSpanish
                ? $"Tus ingresos {direction} un {absChange}% respecto al año anterior."
                : $"Your income {direction} by {absChange}% compared to last year.");
        }
        else
        {
            sentences.Add(isSpanish
                ? $"Tus ingresos totales fueron de {FormatAmount(summary.Income)}."
                : $"Your total income was {FormatAmount(summary.Income)}.");
        }

        // Sentence 2: Savings rate
        if (summary.SavingsRate > 0m)
        {
            string savingsMilestone = summary.SavingsRate switch
            {
                >= 50m => isSpanish
                    ? "excelente: ahorraste más de la mitad de tus ingresos."
                    : "excellent: you saved more than half of your income.",
                >= 30m => isSpanish
                    ? "muy bueno: ahorraste un tercio de tus ingresos."
                    : "very good: you saved around one third of your income.",
                >= 20m => isSpanish
                    ? "saludable: alcanzaste el 20% recomendado de ahorro."
                    : "healthy: you reached the recommended 20% savings threshold.",
                >= 10m => isSpanish
                    ? "moderado: ahorraste una parte de tus ingresos."
                    : "moderate: you saved part of your income.",
                _ => isSpanish
                    ? "bajo: intenta aumentar tu tasa de ahorro."
                    : "low: try to increase your savings rate."
            };
            sentences.Add(isSpanish
                ? $"Tu tasa de ahorro fue del {summary.SavingsRate:F1}%, un nivel {savingsMilestone}"
                : $"Your savings rate was {summary.SavingsRate:F1}%, which is {savingsMilestone}");
        }
        else
        {
            sentences.Add(_localizer["Insights.NoSavings"]);
        }

        // Sentence 3: Category insight (if classified rows exist)
        List<AnnualAnalysisRowDto> expenseRows = classifiedRows
            .Where(r => !r.IsIncome)
            .OrderByDescending(r => r.MonthlyAmounts.Sum())
            .ToList();

        if (expenseRows.Count >= 2)
        {
            decimal totalExpense = expenseRows.Sum(r => r.MonthlyAmounts.Sum());
            decimal top2Sum = expenseRows.Take(2).Sum(r => r.MonthlyAmounts.Sum());
            if (totalExpense > 0m)
            {
                decimal top2Pct = Math.Round(top2Sum / totalExpense * 100m, 1);
                if (top2Pct >= 50m)
                {
                    sentences.Add(isSpanish
                        ? $"Tus dos mayores categorías de gasto representan el {top2Pct}% del total."
                        : $"Your top two spending categories account for {top2Pct}% of total expenses.");
                }
            }
        }

        // Sentence 4: Expense change YoY
        if (summary.HasPreviousYear && summary.ExpenseChangePct.HasValue && Math.Abs(summary.ExpenseChangePct.Value) >= 5m)
        {
            string expenseDirection = summary.ExpenseChangePct.Value >= 0m
                ? (isSpanish ? "aumentaron" : "increased")
                : (isSpanish ? "se redujeron" : "decreased");
            string expenseAbs = Math.Abs(summary.ExpenseChangePct.Value).ToString("F1", CultureInfo.InvariantCulture);
            sentences.Add(isSpanish
                ? $"Tus gastos {expenseDirection} un {expenseAbs}% interanual."
                : $"Your expenses {expenseDirection} by {expenseAbs}% year over year.");
        }

        // Ensure at least 2 sentences
        if (sentences.Count < 2)
        {
            sentences.Add(isSpanish
                ? $"Registraste {ratios.TransactionCount} transacciones en {summary.Year}."
                : $"You recorded {ratios.TransactionCount} transactions in {summary.Year}.");
        }

        return string.Join(" ", sentences);
    }

    private static string FormatAmount(decimal amount)
    {
        return $"€{amount:N2}";
    }

    /// <summary>
    /// Generates deterministic discoveries for REQ-013.
    /// Returns at least one fallback item when data is insufficient.
    /// </summary>
    public IReadOnlyList<DiscoveryDto> GenerateDiscoveries(IReadOnlyList<Transaction> transactions)
    {
        bool isSpanish = string.Equals(
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            "es",
            StringComparison.OrdinalIgnoreCase);

        List<Transaction> expenseTransactions = transactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .ToList();

        if (expenseTransactions.Count < 3)
        {
            return new[]
            {
                new DiscoveryDto(
                    Icon: "ℹ️",
                    Title: _localizer["Discovery.TitleNoDiscoveries"],
                    Description: _localizer["Discovery.InsufficientData"],
                    Category: "insufficient-data")
            };
        }

        List<DiscoveryDto> discoveries = new();

        // 1) Category concentration: top 2 expense categories share
        List<IGrouping<string, Transaction>> groupedByCategory = expenseTransactions
            .GroupBy(GetCategory)
            .ToList();

        List<decimal> categoryTotals = groupedByCategory
            .Select(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
            .OrderByDescending(x => x)
            .ToList();

        decimal totalExpense = categoryTotals.Sum();
        if (categoryTotals.Count >= 2 && totalExpense > 0m)
        {
            decimal top2Pct = Math.Round((categoryTotals[0] + categoryTotals[1]) / totalExpense * 100m, 1);
            discoveries.Add(new DiscoveryDto(
                Icon: "📊",
                Title: _localizer["Discovery.TitleTopCategoriesConcentration"],
                Description: isSpanish
                    ? $"{top2Pct:F1}% de los gastos proviene de tus 2 categorías principales."
                    : $"{top2Pct:F1}% of expenses come from your top 2 categories.",
                Category: "category-concentration"));
        }

        // 2) Highest spending month
        List<IGrouping<int, Transaction>> groupedByMonth = expenseTransactions
            .GroupBy(t => t.Date.Month)
            .ToList();

        if (groupedByMonth.Count > 0)
        {
            IGrouping<int, Transaction> topMonth = groupedByMonth
                .OrderByDescending(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
                .First();
            decimal monthAmount = topMonth.Sum(t => Math.Abs(t.Amount.Amount));
            string monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(topMonth.Key);
            discoveries.Add(new DiscoveryDto(
                Icon: "🗓️",
                Title: _localizer["Discovery.TitleHighestSpendingMonth"],
                Description: isSpanish
                    ? $"{monthName} fue el mes con mayor gasto (€{monthAmount:F2})."
                    : $"{monthName} had the highest expense total (€{monthAmount:F2}).",
                Category: "monthly-pattern"));
        }

        // 3) Weekday pattern
        List<IGrouping<DayOfWeek, Transaction>> groupedByWeekday = expenseTransactions
            .GroupBy(t => t.Date.DayOfWeek)
            .ToList();

        if (groupedByWeekday.Count > 0)
        {
            IGrouping<DayOfWeek, Transaction> topWeekday = groupedByWeekday
                .OrderByDescending(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
                .First();
            decimal weekdayAmount = topWeekday.Sum(t => Math.Abs(t.Amount.Amount));
            discoveries.Add(new DiscoveryDto(
                Icon: "📅",
                Title: _localizer["Discovery.TitleWeekdaySpendingPattern"],
                Description: isSpanish
                    ? $"{topWeekday.Key} es tu día de mayor gasto (€{weekdayAmount:F2})."
                    : $"{topWeekday.Key} is your highest spending weekday (€{weekdayAmount:F2}).",
                Category: "weekday-pattern"));
        }

        // 4) Reducing months streak
        decimal[] monthTotals = new decimal[12];
        foreach (Transaction expenseTransaction in expenseTransactions)
        {
            int monthIndex = expenseTransaction.Date.Month - 1;
            monthTotals[monthIndex] += Math.Abs(expenseTransaction.Amount.Amount);
        }

        int longestReductionStreak = 0;
        int currentStreak = 0;
        for (int i = 1; i < monthTotals.Length; i++)
        {
            if (monthTotals[i] < monthTotals[i - 1] && monthTotals[i - 1] > 0m)
            {
                currentStreak++;
                if (currentStreak > longestReductionStreak)
                {
                    longestReductionStreak = currentStreak;
                }
            }
            else
            {
                currentStreak = 0;
            }
        }

        if (longestReductionStreak >= 2)
        {
            discoveries.Add(new DiscoveryDto(
                Icon: "📉",
                Title: _localizer["Discovery.TitleExpenseReductionStreak"],
                Description: isSpanish
                    ? $"Mantuviste {longestReductionStreak + 1} meses consecutivos reduciendo gastos."
                    : $"You sustained {longestReductionStreak + 1} consecutive months of reducing expenses.",
                Category: "reduction-streak"));
        }

        if (discoveries.Count >= 3)
        {
            return discoveries.AsReadOnly();
        }

        while (discoveries.Count < 3)
        {
            discoveries.Add(new DiscoveryDto(
                Icon: "ℹ️",
                Title: _localizer["Discovery.TitleNoDiscoveries"],
                Description: _localizer["Discovery.InsufficientAdditionalData"],
                Category: "insufficient-data"));
        }

        return discoveries.AsReadOnly();
    }

    private static string GetCategory(Transaction transaction)
    {
        if (!string.IsNullOrWhiteSpace(transaction.BankCategory))
        {
            return transaction.BankCategory.Trim();
        }

        return transaction.Description;
    }
}

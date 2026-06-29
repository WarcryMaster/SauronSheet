namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Domain.Entities;
using DTOs;

/// <summary>
/// Pure anomaly detection service (REQ-008).
/// Rules:
/// - anomaly: current amount > (historical mean + 2σ)
/// - extraordinary: current amount > 3 × historical mean
/// - exceptional: extraordinary spike that does not repeat same month previous year
/// </summary>
public static class AnomalyDetectionService
{
    public static IReadOnlyList<AnomalyDto> Compute(
        Dictionary<int, List<Transaction>> transactionsByYear,
        int selectedYear)
    {
        if (!transactionsByYear.TryGetValue(selectedYear, out List<Transaction>? selectedYearTransactions)
            || selectedYearTransactions.Count == 0)
        {
            return Array.Empty<AnomalyDto>();
        }

        IReadOnlyList<IGrouping<string, Transaction>> categoryGroups = selectedYearTransactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .GroupBy(GetCategoryKey)
            .ToList();

        List<AnomalyDto> anomalies = new();

        foreach (IGrouping<string, Transaction> categoryGroup in categoryGroups)
        {
            for (int month = 1; month <= 12; month++)
            {
                decimal currentAmount = categoryGroup
                    .Where(t => t.Date.Month == month)
                    .Sum(t => Math.Abs(t.Amount.Amount));

                if (currentAmount <= 0m)
                {
                    continue;
                }

                List<decimal> historicalAmounts = BuildHistoricalMonthlySeries(
                    transactionsByYear,
                    selectedYear,
                    categoryGroup.Key,
                    month);

                if (historicalAmounts.Count == 0)
                {
                    continue;
                }

                decimal mean = historicalAmounts.Average();
                decimal stdDev = ComputeStandardDeviation(historicalAmounts, mean);
                decimal anomalyThreshold = mean + (2m * stdDev);

                if (currentAmount <= anomalyThreshold)
                {
                    continue;
                }

                string type = "anomaly";
                if (mean > 0m && currentAmount > (3m * mean))
                {
                    bool repeatedInPreviousYear = IsRepeatedSpikeInPreviousYear(
                        transactionsByYear,
                        selectedYear,
                        categoryGroup.Key,
                        month,
                        mean);

                    type = repeatedInPreviousYear ? "extraordinary" : "exceptional";
                }

                string description = BuildDescription(type, month, currentAmount, mean, stdDev);
                anomalies.Add(new AnomalyDto(
                    Category: categoryGroup.Key,
                    Month: month,
                    Amount: Math.Round(currentAmount, 2),
                    Mean: Math.Round(mean, 2),
                    StandardDeviation: Math.Round(stdDev, 2),
                    Type: type,
                    Description: description));
            }
        }

        return anomalies
            .OrderByDescending(a => a.Amount)
            .ThenBy(a => a.Category, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static List<decimal> BuildHistoricalMonthlySeries(
        Dictionary<int, List<Transaction>> transactionsByYear,
        int selectedYear,
        string category,
        int month)
    {
        List<decimal> amounts = new();

        foreach (KeyValuePair<int, List<Transaction>> kvp in transactionsByYear)
        {
            if (kvp.Key == selectedYear)
            {
                continue;
            }

            decimal amount = kvp.Value
                .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
                .Where(t => string.Equals(GetCategoryKey(t), category, StringComparison.OrdinalIgnoreCase))
                .Where(t => t.Date.Month == month)
                .Sum(t => Math.Abs(t.Amount.Amount));

            if (amount > 0m)
            {
                amounts.Add(amount);
            }
        }

        return amounts;
    }

    private static bool IsRepeatedSpikeInPreviousYear(
        Dictionary<int, List<Transaction>> transactionsByYear,
        int selectedYear,
        string category,
        int month,
        decimal historicalMean)
    {
        int previousYear = selectedYear - 1;
        if (!transactionsByYear.TryGetValue(previousYear, out List<Transaction>? previousYearTransactions)
            || previousYearTransactions.Count == 0)
        {
            return false;
        }

        decimal previousAmount = previousYearTransactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .Where(t => string.Equals(GetCategoryKey(t), category, StringComparison.OrdinalIgnoreCase))
            .Where(t => t.Date.Month == month)
            .Sum(t => Math.Abs(t.Amount.Amount));

        return previousAmount > 0m;
    }

    private static decimal ComputeStandardDeviation(IReadOnlyList<decimal> values, decimal mean)
    {
        if (values.Count <= 1)
        {
            return 0m;
        }

        decimal variance = values
            .Select(v => (v - mean) * (v - mean))
            .Average();

        return (decimal)Math.Sqrt((double)variance);
    }

    private static string BuildDescription(string type, int month, decimal amount, decimal mean, decimal stdDev)
    {
        string monthLabel = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month);

        return type switch
        {
            "exceptional" => $"{monthLabel}: exceptional spike (€{amount:F2}) with no repeated spike in previous year.",
            "extraordinary" => $"{monthLabel}: extraordinary expense (€{amount:F2}) above 3× mean (€{mean:F2}).",
            _ => $"{monthLabel}: anomaly (€{amount:F2}) above μ+2σ threshold (€{(mean + 2m * stdDev):F2}).",
        };
    }

    private static string GetCategoryKey(Transaction transaction)
    {
        if (!string.IsNullOrWhiteSpace(transaction.BankCategory))
        {
            return transaction.BankCategory.Trim();
        }

        return transaction.Description;
    }
}

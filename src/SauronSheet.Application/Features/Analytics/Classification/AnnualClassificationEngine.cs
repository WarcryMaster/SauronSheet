namespace SauronSheet.Application.Features.Analytics.Classification;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Domain.Entities;
using Domain.ValueObjects;
using DTOs;
using Helpers;

/// <summary>
/// Pure engine that classifies transactions into fixed/variable income/expense rows.
/// No external dependencies, no persistence, no Sentry.
/// </summary>
public class AnnualClassificationEngine : IAnnualClassificationEngine
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.Compiled,
        RegexTimeout);

    private static readonly HashSet<string> FixedExpenseMappings = new(StringComparer.InvariantCulture)
    {
        "luz y gas",
        "agua",
        "suscripciones",
        "ong",
        "telefono tv e internet"
    };

    private static readonly HashSet<string> VariableExpenseMappings = new(StringComparer.InvariantCulture)
    {
        "supermercados y alimentacion",
        "cafeterias y restaurantes",
        "gasolina y combustible",
        "ropa y complementos",
        "ocio y viajes otros"
    };

    private const string IncomeVariableMapping = "ingresos de otras entidades";
    private const string UncategorizedLabel = "Sin clasificar";

    /// <summary>
    /// Normalizes a text: lower-case invariant, removes diacritics and collapses whitespace.
    /// </summary>
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string lower = input.ToLowerInvariant();
        string formD = lower.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(formD.Length);

        foreach (char character in formD)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        string withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC);
        return WhitespaceRegex.Replace(withoutDiacritics, " ").Trim();
    }

    /// <summary>
    /// Determines whether a set of absolute amounts is recurrent enough to be considered fixed.
    /// Requires at least 3 distinct months and a coefficient of variation (population) &lt;= 10%.
    /// </summary>
    public static bool IsRecurrentFixed(IReadOnlyList<decimal> amounts, int distinctMonths)
    {
        if (distinctMonths < 3 || amounts.Count == 0)
        {
            return false;
        }

        decimal mean = amounts.Average();
        if (mean == 0m)
        {
            return false;
        }

        decimal sumOfSquaredDifferences = amounts.Sum(amount => (amount - mean) * (amount - mean));
        decimal variance = sumOfSquaredDifferences / amounts.Count;
        if (variance < 0m)
        {
            variance = 0m;
        }

        decimal standardDeviation = (decimal)Math.Sqrt((double)variance);
        decimal coefficientOfVariation = standardDeviation / mean;
        return coefficientOfVariation <= 0.10m;
    }

    /// <inheritdoc />
    public IReadOnlyList<AnnualAnalysisRowDto> Classify(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyDictionary<SubcategoryId, string> subcategoryNames,
        int year)
    {
        List<AnnualAnalysisRowDto> rows = new();

        IEnumerable<Transaction> relevantTransactions = transactions
            .Where(transaction => !transaction.Amount.IsZero)
            .Where(transaction => transaction.Date.ToSpainLocal().Year == year);

        IEnumerable<IGrouping<ClassificationKey, Transaction>> groups = relevantTransactions
            .GroupBy(transaction => new ClassificationKey(
                transaction.SubcategoryId,
                transaction.Amount.IsPositive));

        foreach (IGrouping<ClassificationKey, Transaction> group in groups)
        {
            SubcategoryId? subcategoryId = group.Key.SubcategoryId;
            bool isIncome = group.Key.IsIncome;

            string movement = ResolveMovement(subcategoryId, subcategoryNames);
            AnalysisLineType lineType = ResolveLineType(isIncome, subcategoryId, movement, group);

            decimal[] monthlyAmounts = new decimal[12];
            foreach (Transaction transaction in group)
            {
                int month = transaction.Date.ToSpainLocal().Month;
                monthlyAmounts[month - 1] += Math.Abs(transaction.Amount.Amount);
            }

            decimal annualSum = monthlyAmounts.Sum();
            decimal average = annualSum / 12m;
            string typeLabel = lineType.GetTypeLabel();
            string currency = group.First().Amount.Currency;

            rows.Add(new AnnualAnalysisRowDto(
                movement,
                lineType,
                typeLabel,
                average,
                monthlyAmounts,
                currency));
        }

        return rows
            .OrderBy(row => row.LineType)
            .ThenBy(row => row.Movement)
            .ToList()
            .AsReadOnly();
    }

    private static string ResolveMovement(
        SubcategoryId? subcategoryId,
        IReadOnlyDictionary<SubcategoryId, string> subcategoryNames)
    {
        if (subcategoryId == null)
        {
            return UncategorizedLabel;
        }

        return subcategoryNames.TryGetValue(subcategoryId, out string? name)
            ? name
            : UncategorizedLabel;
    }

    private static AnalysisLineType ResolveLineType(
        bool isIncome,
        SubcategoryId? subcategoryId,
        string movement,
        IEnumerable<Transaction> groupTransactions)
    {
        if (isIncome)
        {
            if (subcategoryId == null)
            {
                return AnalysisLineType.IncomeVariable;
            }

            string normalizedName = Normalize(movement);
            if (normalizedName == IncomeVariableMapping)
            {
                return AnalysisLineType.IncomeVariable;
            }

            return AnalysisLineType.IncomeFixed;
        }

        if (subcategoryId == null)
        {
            return AnalysisLineType.ExpenseVariable;
        }

        string normalizedExpenseName = Normalize(movement);
        if (FixedExpenseMappings.Contains(normalizedExpenseName))
        {
            return AnalysisLineType.ExpenseFixed;
        }

        if (VariableExpenseMappings.Contains(normalizedExpenseName))
        {
            return AnalysisLineType.ExpenseVariable;
        }

        return ResolveHeuristicLineType(groupTransactions);
    }

    private static AnalysisLineType ResolveHeuristicLineType(IEnumerable<Transaction> groupTransactions)
    {
        IEnumerable<IGrouping<string, Transaction>> descriptionGroups = groupTransactions
            .GroupBy(transaction => Normalize(transaction.Description));

        foreach (IGrouping<string, Transaction> descriptionGroup in descriptionGroups)
        {
            int distinctMonths = descriptionGroup
                .Select(transaction => transaction.Date.ToSpainLocal().Month)
                .Distinct()
                .Count();

            List<decimal> amounts = descriptionGroup
                .Select(transaction => Math.Abs(transaction.Amount.Amount))
                .ToList();

            if (IsRecurrentFixed(amounts, distinctMonths))
            {
                return AnalysisLineType.ExpenseFixed;
            }
        }

        return AnalysisLineType.ExpenseVariable;
    }

    private readonly record struct ClassificationKey(SubcategoryId? SubcategoryId, bool IsIncome);
}

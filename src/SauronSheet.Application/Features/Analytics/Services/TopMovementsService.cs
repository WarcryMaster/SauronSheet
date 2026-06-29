namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.ValueObjects;
using DTOs;

/// <summary>
/// Result from TopMovementsService containing top expenses, incomes, and most frequent movements.
/// </summary>
public record TopMovementsResult(
    IReadOnlyList<TopMovementDto> TopExpenses,
    IReadOnlyList<TopMovementDto> TopIncomes,
    IReadOnlyList<TopMovementDto> MostFrequent);

/// <summary>
/// Pure service that computes top movements from transactions (REQ-010).
/// Returns top 10 expenses, top 10 incomes, and most frequent descriptions.
/// No external dependencies.
/// </summary>
public static class TopMovementsService
{
    private const int TopN = 10;

    /// <summary>
    /// Computes top movements from the given year's transactions.
    /// </summary>
    /// <param name="transactions">All transactions for the selected year.</param>
    /// <param name="year">The selected year (used for date formatting).</param>
    /// <returns>TopMovementsResult with expense, income, and frequent movements.</returns>
    public static TopMovementsResult Compute(
        IReadOnlyList<Transaction> transactions,
        int year)
    {
        // Top expenses (by absolute amount, descending)
        List<TopMovementDto> topExpenses = transactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .OrderBy(t => t.Amount.Amount) // Most negative first
            .Take(TopN)
            .Select(t => new TopMovementDto(
                Description: t.Description,
                Amount: Math.Abs(t.Amount.Amount),
                Date: t.Date.ToString("yyyy-MM-dd"),
                Category: t.SubcategoryId?.Value.ToString() ?? "Uncategorized",
                Type: "expense",
                TransactionId: t.Id.Value.ToString()))
            .ToList();

        // Top incomes (by amount, descending)
        List<TopMovementDto> topIncomes = transactions
            .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
            .OrderByDescending(t => t.Amount.Amount)
            .Take(TopN)
            .Select(t => new TopMovementDto(
                Description: t.Description,
                Amount: t.Amount.Amount,
                Date: t.Date.ToString("yyyy-MM-dd"),
                Category: t.SubcategoryId?.Value.ToString() ?? "Uncategorized",
                Type: "income",
                TransactionId: t.Id.Value.ToString()))
            .ToList();

        // Most frequent descriptions
        List<TopMovementDto> mostFrequent = transactions
            .Where(t => !t.Amount.IsZero)
            .GroupBy(t => t.Description, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
            .Take(TopN)
            .Select(g => new TopMovementDto(
                Description: g.Key,
                Amount: Math.Round(g.Average(t => Math.Abs(t.Amount.Amount)), 2),
                Date: g.First().Date.ToString("yyyy-MM-dd"),
                Category: g.First().SubcategoryId?.Value.ToString() ?? "Uncategorized",
                Type: "frequent",
                TransactionId: null))
            .ToList();

        return new TopMovementsResult(
            TopExpenses: topExpenses.AsReadOnly(),
            TopIncomes: topIncomes.AsReadOnly(),
            MostFrequent: mostFrequent.AsReadOnly());
    }
}

namespace SauronSheet.Application.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using DTOs;

/// <summary>
/// Pure service that generates chronological timeline events from transactions (REQ-009).
/// Events: highest income, biggest expense, savings record, first/last transaction.
/// Returns empty list when no transactions exist.
/// No external dependencies.
/// </summary>
public static class TimelineService
{
    /// <summary>
    /// Computes timeline events from the given year's transactions.
    /// </summary>
    /// <param name="transactions">All transactions for the selected year.</param>
    /// <param name="year">The selected year (for net computation).</param>
    /// <returns>List of timeline events, sorted chronologically.</returns>
    public static IReadOnlyList<TimelineEventDto> Compute(
        IReadOnlyList<Transaction> transactions,
        int year)
    {
        if (transactions.Count == 0)
        {
            return Array.Empty<TimelineEventDto>();
        }

        List<TimelineEventDto> events = new();

        // Find highest income transaction
        Transaction? highestIncome = transactions
            .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
            .OrderByDescending(t => t.Amount.Amount)
            .FirstOrDefault();

        if (highestIncome != null)
        {
            events.Add(new TimelineEventDto(
                Type: "highest-income",
                Label: "Highest Income",
                Description: $"{highestIncome.Description} — €{Math.Abs(highestIncome.Amount.Amount):N2}",
                Date: highestIncome.Date.ToString("yyyy-MM-dd"),
                Amount: Math.Abs(highestIncome.Amount.Amount),
                Icon: "arrow-up"));
        }

        // Find biggest expense transaction
        Transaction? biggestExpense = transactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .OrderByDescending(t => Math.Abs(t.Amount.Amount))
            .FirstOrDefault();

        if (biggestExpense != null)
        {
            events.Add(new TimelineEventDto(
                Type: "biggest-expense",
                Label: "Biggest Expense",
                Description: $"{biggestExpense.Description} — €{Math.Abs(biggestExpense.Amount.Amount):N2}",
                Date: biggestExpense.Date.ToString("yyyy-MM-dd"),
                Amount: Math.Abs(biggestExpense.Amount.Amount),
                Icon: "arrow-down"));
        }

        // Find first and last transaction of the year
        Transaction? firstTrx = transactions
            .OrderBy(t => t.Date)
            .FirstOrDefault();

        Transaction? lastTrx = transactions
            .OrderByDescending(t => t.Date)
            .FirstOrDefault();

        if (firstTrx != null && firstTrx != highestIncome && firstTrx != biggestExpense)
        {
            events.Add(new TimelineEventDto(
                Type: "first-transaction",
                Label: "First Transaction",
                Description: $"{firstTrx.Description} — €{Math.Abs(firstTrx.Amount.Amount):N2}",
                Date: firstTrx.Date.ToString("yyyy-MM-dd"),
                Amount: Math.Abs(firstTrx.Amount.Amount),
                Icon: "start"));
        }

        if (lastTrx != null && lastTrx != highestIncome && lastTrx != biggestExpense && lastTrx != firstTrx)
        {
            events.Add(new TimelineEventDto(
                Type: "last-transaction",
                Label: "Last Transaction",
                Description: $"{lastTrx.Description} — €{Math.Abs(lastTrx.Amount.Amount):N2}",
                Date: lastTrx.Date.ToString("yyyy-MM-dd"),
                Amount: Math.Abs(lastTrx.Amount.Amount),
                Icon: "end"));
        }

        // Net savings milestone (if positive)
        decimal totalIncome = transactions
            .Where(t => t.Amount.IsPositive && !t.Amount.IsZero)
            .Sum(t => Math.Abs(t.Amount.Amount));

        decimal totalExpense = transactions
            .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
            .Sum(t => Math.Abs(t.Amount.Amount));

        decimal netSavings = totalIncome - totalExpense;
        if (netSavings > 0m && events.Count > 0)
        {
            events.Add(new TimelineEventDto(
                Type: "savings-record",
                Label: "Net Savings",
                Description: $"Total net savings of €{netSavings:N2} for {year}",
                Date: year.ToString(),
                Amount: netSavings,
                Icon: "piggy-bank"));
        }

        // Sort chronologically (events without specific dates go last)
        return events
            .OrderBy(e => e.Date)
            .ToList()
            .AsReadOnly();
    }
}

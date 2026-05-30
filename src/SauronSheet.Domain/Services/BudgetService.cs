namespace SauronSheet.Domain.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using Exceptions;
using Repositories;
using ValueObjects;

/// <summary>
/// Domain service for cross-entity budget logic.
/// Handles overlap validation (one active budget per user + category at any point in time)
/// and budget status level calculation (Green/Yellow/Red/Overage).
/// </summary>
public class BudgetService
{
    private readonly IBudgetRepository _budgetRepository;

    public BudgetService(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
    }

    /// <summary>
    /// Validates that a new or updated budget does not overlap in time with any
    /// existing budget for the same user and category.
    /// </summary>
    /// <param name="userId">Owner of the budget.</param>
    /// <param name="categoryId">Spending category.</param>
    /// <param name="from">Effective start date of the new/updated budget (inclusive).</param>
    /// <param name="until">
    /// Effective end date of the new/updated budget (inclusive).
    /// Pass null for a permanent budget with no end date.
    /// </param>
    /// <param name="excludeBudgetId">
    /// Optional BudgetId to exclude from overlap check (used when updating an existing budget).
    /// </param>
    /// <exception cref="DomainException">
    /// Thrown when an existing budget's effective range overlaps with [from, until].
    /// </exception>
    public async Task ValidateNoOverlap(
        UserId userId,
        CategoryId categoryId,
        DateOnly from,
        DateOnly? until,
        BudgetId? excludeBudgetId = null)
    {
        IReadOnlyList<Budget> existing = await _budgetRepository.GetByUserAndCategoryAsync(userId, categoryId);

        foreach (Budget budget in existing)
        {
            // Skip the budget being updated
            if (excludeBudgetId is not null && budget.Id == excludeBudgetId)
                continue;

            if (RangesOverlap(
                from, until,
                budget.EffectiveFrom, budget.EffectiveUntil))
            {
                throw new DomainException(
                    "A budget for this category with an overlapping date range already exists. " +
                    $"Existing budget effective from {budget.EffectiveFrom:yyyy-MM-dd}" +
                    (budget.EffectiveUntil.HasValue
                        ? $" to {budget.EffectiveUntil:yyyy-MM-dd}"
                        : " (permanent)") +
                    ".");
            }
        }
    }

    /// <summary>
    /// Determines whether two date ranges overlap.
    /// Two ranges overlap if Range A starts on or before Range B's end
    /// AND Range A ends on or after Range B's start.
    /// Adjacent ranges (e.g., A ends 2026-06-30, B starts 2026-07-01) do NOT overlap.
    /// A null end date is treated as an infinite (unbounded) range.
    /// </summary>
    private static bool RangesOverlap(
        DateOnly rangeAFrom, DateOnly? rangeAUntil,
        DateOnly rangeBFrom, DateOnly? rangeBUntil)
    {
        // Range A starts after Range B ends → no overlap
        if (rangeBUntil.HasValue && rangeAFrom > rangeBUntil.Value)
            return false;

        // Range A ends before Range B starts → no overlap
        if (rangeAUntil.HasValue && rangeAUntil.Value < rangeBFrom)
            return false;

        // Otherwise, they overlap
        return true;
    }

    /// <summary>
    /// Calculate the budget status level based on percentage used.
    /// Thresholds: Green &lt; 75%, Yellow 75%–&lt;100%, Red = 100% exactly, Overage &gt; 100%.
    /// </summary>
    public static BudgetStatusLevel GetStatusLevel(decimal percentageUsed)
    {
        return percentageUsed switch
        {
            > 1.0m   => BudgetStatusLevel.Overage,
            1.0m     => BudgetStatusLevel.Red,
            >= 0.75m => BudgetStatusLevel.Yellow,
            _        => BudgetStatusLevel.Green
        };
    }
}

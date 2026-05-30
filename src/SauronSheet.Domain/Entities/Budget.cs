namespace SauronSheet.Domain.Entities;

using System;
using Common;
using Exceptions;
using ValueObjects;

/// <summary>
/// Budget aggregate root — redesigned as a permanent policy with configurable granularity.
/// A Budget defines a spending limit per period (Monthly, Quarterly, Semester, Annual)
/// that applies continuously from EffectiveFrom until deactivated (EffectiveUntil)
/// or modified. Replaces the old monthly-budget model based on DateRange.
/// </summary>
public class Budget : AggregateRoot<BudgetId>
{
    public UserId UserId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveUntil { get; private set; }
    public BudgetPeriod PeriodGranularity { get; private set; }
    public Money Limit { get; private set; }

    /// <summary>
    /// Creates a new budget policy.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="userId">Owner of the budget.</param>
    /// <param name="categoryId">Associated spending category.</param>
    /// <param name="effectiveFrom">Date from which this budget applies (inclusive).</param>
    /// <param name="effectiveUntil">
    /// Date until which this budget applies (inclusive).
    /// Pass null for a permanent budget that never expires.
    /// </param>
    /// <param name="periodGranularity">Time granularity for the limit (Monthly, Quarterly, Semester, Annual).</param>
    /// <param name="limit">Spending limit per period of the chosen granularity. Must be positive.</param>
    /// <exception cref="ArgumentNullException">Thrown when userId, categoryId, or limit is null.</exception>
    /// <exception cref="DomainException">Thrown when limit is not positive or effectiveUntil is before effectiveFrom.</exception>
    public Budget(
        BudgetId id,
        UserId userId,
        CategoryId categoryId,
        DateOnly effectiveFrom,
        DateOnly? effectiveUntil,
        BudgetPeriod periodGranularity,
        Money limit)
        : base(id)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));

        if (effectiveUntil.HasValue && effectiveUntil.Value < effectiveFrom)
            throw new DomainException(
                "EffectiveUntil must be on or after EffectiveFrom.");

        if (limit == null) throw new ArgumentNullException(nameof(limit));
        if (!limit.IsPositive)
            throw new DomainException("Budget limit must be positive.");

        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
        PeriodGranularity = periodGranularity;
        Limit = limit;
    }

    /// <summary>
    /// Updates the spending limit per period. New limit must be positive.
    /// </summary>
    public void UpdateLimit(Money newLimit)
    {
        if (newLimit == null) throw new ArgumentNullException(nameof(newLimit));
        if (!newLimit.IsPositive)
            throw new DomainException("Budget limit must be positive.");

        Limit = newLimit;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the effective date range for this budget policy.
    /// </summary>
    /// <param name="from">New start date (inclusive).</param>
    /// <param name="until">New end date (inclusive), or null for a permanent budget.</param>
    /// <exception cref="DomainException">Thrown when until is before from.</exception>
    public void UpdateEffectiveDates(DateOnly from, DateOnly? until)
    {
        if (until.HasValue && until.Value < from)
            throw new DomainException(
                "EffectiveUntil must be on or after EffectiveFrom.");

        EffectiveFrom = from;
        EffectiveUntil = until;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the period granularity of this budget (e.g., from Monthly to Annual).
    /// </summary>
    public void UpdateGranularity(BudgetPeriod newGranularity)
    {
        PeriodGranularity = newGranularity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this budget by setting EffectiveUntil to the given date.
    /// The budget will no longer apply to dates after the deactivation date.
    /// </summary>
    /// <param name="asOf">Date from which this budget is deactivated.</param>
    /// <exception cref="DomainException">Thrown when asOf is before EffectiveFrom.</exception>
    public void Deactivate(DateOnly asOf)
    {
        if (asOf < EffectiveFrom)
            throw new DomainException(
                "Deactivation date cannot be before EffectiveFrom.");

        EffectiveUntil = asOf;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks whether this budget is active on a specific date.
    /// A permanent budget (EffectiveUntil = null) is active for any date
    /// on or after EffectiveFrom.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>
    /// True if the date falls within [EffectiveFrom, EffectiveUntil]
    /// (or EffectiveFrom onward when EffectiveUntil is null).
    /// </returns>
    public bool IsActiveOn(DateOnly date)
    {
        if (date < EffectiveFrom)
            return false;

        if (EffectiveUntil.HasValue && date > EffectiveUntil.Value)
            return false;

        return true;
    }
}

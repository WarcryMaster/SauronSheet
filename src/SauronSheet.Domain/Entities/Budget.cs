namespace SauronSheet.Domain.Entities;

using System;
using Common;
using Exceptions;
using ValueObjects;

/// <summary>
/// Budget aggregate root.
/// Represents a monthly spending limit for a specific category.
/// One budget per user-category-month (uniqueness enforced by repository + DB constraint).
/// </summary>
public class Budget : AggregateRoot<BudgetId>
{
    public UserId UserId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public DateRange Period { get; private set; }
    public Money Limit { get; private set; }

    public Budget(
        BudgetId id,
        UserId userId,
        CategoryId categoryId,
        DateRange period,
        Money limit)
        : base(id)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        Period = period ?? throw new ArgumentNullException(nameof(period));

        if (limit == null) throw new ArgumentNullException(nameof(limit));
        if (!limit.IsPositive)
            throw new DomainException("Budget limit must be positive.");

        Limit = limit;
    }

    /// <summary>
    /// Whether current spending exceeds the budget limit.
    /// Returns true only when spend strictly exceeds limit.
    /// </summary>
    public bool IsOverBudget(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        EnsureSameCurrency(currentSpend);
        return currentSpend.Amount > Limit.Amount;
    }

    /// <summary>
    /// Percentage of budget used (0.0 = 0%, 1.0 = 100%, >1.0 = overage).
    /// Domain returns raw value; UI is responsible for capping display.
    /// </summary>
    public decimal PercentageUsed(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        EnsureSameCurrency(currentSpend);
        if (Limit.Amount == 0) return 0; // Defensive: constructor prevents limit <= 0
        return currentSpend.Amount / Limit.Amount;
    }

    /// <summary>
    /// Remaining budget amount (Limit - currentSpend).
    /// Returns negative Money when over budget.
    /// </summary>
    public Money RemainingAmount(Money currentSpend)
    {
        if (currentSpend == null) throw new ArgumentNullException(nameof(currentSpend));
        // Currency validation delegated to Money.Minus (throws InvalidOperationException)
        return Limit.Minus(currentSpend);
    }

    /// <summary>
    /// Update the budget limit. Throws if new limit is zero or negative.
    /// Sets UpdatedAt timestamp.
    /// </summary>
    public void UpdateLimit(Money newLimit)
    {
        if (newLimit == null) throw new ArgumentNullException(nameof(newLimit));
        if (!newLimit.IsPositive)
            throw new DomainException("Budget limit must be positive.");

        Limit = newLimit;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Limit.Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot compare budget in {Limit.Currency} with spending in {other.Currency}");
    }
}

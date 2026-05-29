namespace SauronSheet.Domain.Services;

using System;
using System.Threading.Tasks;
using Entities;
using Exceptions;
using Repositories;
using ValueObjects;

/// <summary>
/// Domain service for cross-entity budget logic.
/// Handles uniqueness validation (budget + category + month per user)
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
    /// Validate that no budget already exists for the same user, category, and period.
    /// Throws DomainException if a duplicate is found.
    /// </summary>
    public async Task ValidateUniqueBudget(UserId userId, CategoryId categoryId, DateRange period)
    {
        var existing = await _budgetRepository.GetByUserAndCategoryAndMonthAsync(userId, categoryId, period);
        if (existing is not null)
            throw new DomainException(
                $"A budget for this category in {period.StartDate:MMMM yyyy} already exists.");
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

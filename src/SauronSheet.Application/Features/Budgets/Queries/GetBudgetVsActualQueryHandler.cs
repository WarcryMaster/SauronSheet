namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Services;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetBudgetVsActualQuery.
/// Compares budgets vs. actual spending per category.
/// Includes categories with spending but no budget.
/// Sorted by over-budget first, then percentage descending.
/// </summary>
public class GetBudgetVsActualQueryHandler : IRequestHandler<GetBudgetVsActualQuery, List<BudgetVsActualDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetVsActualQueryHandler(
        IBudgetRepository budgetRepo,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<BudgetVsActualDto>> Handle(
        GetBudgetVsActualQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Construct DateRange from year + month
        var periodStart = new DateTime(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Load all user budgets for the period
        var allBudgets = await _budgetRepo.GetByUserIdAsync(userId);
        var periodBudgets = allBudgets
            .Where(b => b.Period.StartDate.Year == request.Year
                     && b.Period.StartDate.Month == request.Month)
            .ToList();

        // Load all transactions for the period
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(periodStart, periodEnd);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);
        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Only expenses
        var expenses = transactions.Where(t => t.Amount.IsNegative).ToList();

        // Group expenses by category (filter out uncategorized)
        var spendByCategory = expenses
            .Where(t => t.CategoryId != null)
            .GroupBy(t => t.CategoryId!)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        // Load categories for lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        var results = new List<BudgetVsActualDto>();

        // Budgeted categories
        var budgetedCategoryIds = new HashSet<CategoryId>();
        foreach (var budget in periodBudgets)
        {
            budgetedCategoryIds.Add(budget.CategoryId);
            var actualSpend = spendByCategory.GetValueOrDefault(budget.CategoryId, 0m);
            var difference = budget.Limit.Amount - actualSpend;
            var currentSpend = new Money(actualSpend);
            var percentageUsed = budget.PercentageUsed(currentSpend);
            var statusLevel = BudgetService.GetStatusLevel(percentageUsed);

            var catName = "Unknown";
            string? catColor = null;
            if (categoryLookup.TryGetValue(budget.CategoryId, out var cat))
            {
                catName = cat.Name.Value;
                catColor = cat.Color.Value;
            }

            results.Add(new BudgetVsActualDto(
                budget.CategoryId.Value,
                catName,
                catColor,
                budget.Limit.Amount,
                actualSpend,
                difference,
                percentageUsed,
                statusLevel.ToString(),
                budget.Limit.Currency));
        }

        // Unbudgeted categories with spending
        foreach (var kvp in spendByCategory)
        {
            if (kvp.Key == null || budgetedCategoryIds.Contains(kvp.Key)) continue;

            var catName = "Uncategorized";
            string? catColor = null;
            if (categoryLookup.TryGetValue(kvp.Key, out var cat))
            {
                catName = cat.Name.Value;
                catColor = cat.Color.Value;
            }

            results.Add(new BudgetVsActualDto(
                kvp.Key.Value,
                catName,
                catColor,
                null,
                kvp.Value,
                null,
                null,
                null,
                "EUR"));
        }

        // Sort: over-budget first, then by percentage descending
        return results
            .OrderByDescending(r => r.PercentageUsed.HasValue && r.PercentageUsed > 1.0m)
            .ThenByDescending(r => r.PercentageUsed ?? 0)
            .ToList();
    }
}

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
/// Handler for GetBudgetSummaryForDashboardQuery.
/// Aggregates budget statuses for the current month's dashboard widget.
/// Calculates current spend from transactions per budget.
/// </summary>
public class GetBudgetSummaryForDashboardQueryHandler
    : IRequestHandler<GetBudgetSummaryForDashboardQuery, BudgetDashboardSummaryDto>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetSummaryForDashboardQueryHandler(
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

    public async Task<BudgetDashboardSummaryDto> Handle(
        GetBudgetSummaryForDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Construct DateRange from year + month
        var periodStart = new DateTime(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Load budgets for the period
        var allBudgets = await _budgetRepo.GetByUserIdAsync(userId);
        var periodBudgets = allBudgets
            .Where(b => b.Period.StartDate.Year == request.Year
                     && b.Period.StartDate.Month == request.Month)
            .ToList();

        if (!periodBudgets.Any())
        {
            return new BudgetDashboardSummaryDto(
                new List<BudgetStatusDto>(), 0, 0, 0);
        }

        // Load all transactions for the period (single query, then filter in-memory)
        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(periodStart, periodEnd);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);
        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Group expenses by category (filter out uncategorized)
        var spendByCategory = transactions
            .Where(t => t.Amount.IsNegative && t.CategoryId != null)
            .GroupBy(t => t.CategoryId!)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        // Load categories for lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        var budgetStatuses = new List<BudgetStatusDto>();
        var overBudgetCount = 0;

        foreach (var budget in periodBudgets)
        {
            var actualSpend = spendByCategory.GetValueOrDefault(budget.CategoryId, 0m);
            var currentSpend = new Money(actualSpend);
            var percentageUsed = budget.PercentageUsed(currentSpend);
            var remaining = budget.RemainingAmount(currentSpend);
            var statusLevel = BudgetService.GetStatusLevel(percentageUsed);

            if (statusLevel == BudgetStatusLevel.Overage)
                overBudgetCount++;

            var catName = "Unknown";
            string? catColor = null;
            if (categoryLookup.TryGetValue(budget.CategoryId, out var cat))
            {
                catName = cat.Name;
                catColor = cat.Color;
            }

            budgetStatuses.Add(new BudgetStatusDto(
                budget.Id.Value,
                budget.CategoryId.Value,
                catName,
                catColor,
                budget.Limit.Amount,
                actualSpend,
                remaining.Amount,
                percentageUsed,
                statusLevel.ToString(),
                budget.Limit.Currency,
                budget.Period.StartDate,
                budget.Period.EndDate));
        }

        return new BudgetDashboardSummaryDto(
            budgetStatuses,
            periodBudgets.Count,
            periodBudgets.Count - overBudgetCount,
            overBudgetCount);
    }
}

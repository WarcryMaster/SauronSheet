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
/// Handler for GetBudgetsQuery.
/// Returns budget list with status (CurrentSpend, Remaining, PercentageUsed, StatusLevel).
/// Optional year/month filtering, sorted alphabetically by category name.
/// </summary>
public class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, List<BudgetStatusDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetsQueryHandler(
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

    public async Task<List<BudgetStatusDto>> Handle(GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgets = await _budgetRepo.GetByUserIdAsync(userId);

        // Optional year/month filter
        if (request.Year.HasValue && request.Month.HasValue)
        {
            budgets = budgets
                .Where(b => b.Period.StartDate.Year == request.Year.Value
                         && b.Period.StartDate.Month == request.Month.Value)
                .ToList();
        }
        else if (request.Year.HasValue)
        {
            budgets = budgets
                .Where(b => b.Period.StartDate.Year == request.Year.Value)
                .ToList();
        }

        // Load categories for name/color lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        var results = new List<BudgetStatusDto>();
        foreach (var budget in budgets)
        {
            // Calculate current spend from transactions in this category and period
            var userSpec = new TransactionByUserSpecification(userId);
            var dateSpec = new TransactionByDateRangeSpecification(budget.Period.StartDate, budget.Period.EndDate);
            var categorySpec = new TransactionByCategorySpecification(budget.CategoryId);
            var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(
                CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec),
                categorySpec);

            var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);
            var currentSpendAmount = transactions
                .Where(t => t.Amount.IsNegative)
                .Sum(t => Math.Abs(t.Amount.Amount));

            var currentSpend = new Money(currentSpendAmount);
            var percentageUsed = budget.PercentageUsed(currentSpend);
            var remaining = budget.RemainingAmount(currentSpend);
            var statusLevel = BudgetService.GetStatusLevel(percentageUsed);

            var catName = "Unknown";
            string? catColor = null;
            if (categoryLookup.TryGetValue(budget.CategoryId, out var cat))
            {
                catName = cat.Name;
                catColor = cat.Color;
            }

            results.Add(new BudgetStatusDto(
                budget.Id.Value,
                budget.CategoryId.Value,
                catName,
                catColor,
                budget.Limit.Amount,
                currentSpendAmount,
                remaining.Amount,
                percentageUsed,
                statusLevel.ToString(),
                budget.Limit.Currency,
                budget.Period.StartDate,
                budget.Period.EndDate));
        }

        return results
            .OrderBy(b => b.CategoryName)
            .ToList();
    }
}

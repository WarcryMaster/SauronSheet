namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetBudgetByIdQuery.
/// Calculates current spend from transactions and returns budget with status.
/// </summary>
public class GetBudgetByIdQueryHandler : IRequestHandler<GetBudgetByIdQuery, BudgetStatusDto>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetByIdQueryHandler(
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

    public async Task<BudgetStatusDto> Handle(GetBudgetByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgetId = new BudgetId(request.BudgetId);

        var budget = await _budgetRepo.GetByIdAsync(budgetId);
        if (budget is null || budget.UserId.Value != userId.Value)
            throw new EntityNotFoundException("Budget", request.BudgetId);

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

        // Get category info
        var category = await _categoryRepo.GetByIdAsync(budget.CategoryId);

        return new BudgetStatusDto(
            budget.Id.Value,
            budget.CategoryId.Value,
            category?.Name ?? "Unknown",
            category?.Color,
            budget.Limit.Amount,
            currentSpendAmount,
            remaining.Amount,
            percentageUsed,
            statusLevel.ToString(),
            budget.Limit.Currency,
            budget.Period.StartDate,
            budget.Period.EndDate);
    }
}

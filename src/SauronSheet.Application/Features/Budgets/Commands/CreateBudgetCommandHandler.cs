namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using MediatR;

/// <summary>
/// Handler for CreateBudgetCommand.
/// Validates category existence, budget uniqueness, then creates budget.
/// </summary>
public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, Guid>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly BudgetService _budgetService;
    private readonly IUserContext _userContext;

    public CreateBudgetCommandHandler(
        IBudgetRepository budgetRepo,
        ICategoryRepository categoryRepo,
        BudgetService budgetService,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _categoryRepo = categoryRepo;
        _budgetService = budgetService;
        _userContext = userContext;
    }

    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var categoryId = new CategoryId(request.CategoryId);

        // Validate category exists for this user
        // Feature 3: Safe null-checking for nullable UserId
        var category = await _categoryRepo.GetByIdAsync(categoryId);
        if (category is null || !category.IsAccessibleToUser(userId))
            throw new EntityNotFoundException("Category", request.CategoryId);

        // Build domain values — normalise the period to canonical month boundaries on the
        // server so that timezone drift in the client-submitted dates cannot produce a
        // budget that belongs to the wrong month.
        var normalizedStart = new DateTime(request.PeriodStart.Year, request.PeriodStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var normalizedEnd   = normalizedStart.AddMonths(1).AddDays(-1);
        var period = new DateRange(normalizedStart, normalizedEnd);
        var limit = new Money(request.LimitAmount);

        // Validate uniqueness via domain service
        await _budgetService.ValidateUniqueBudget(userId, categoryId, period);

        // Create aggregate
        var budgetId = new BudgetId(Guid.NewGuid());
        var budget = new Budget(budgetId, userId, categoryId, period, limit);

        await _budgetRepo.AddAsync(budget);
        return budgetId.Value;
    }
}

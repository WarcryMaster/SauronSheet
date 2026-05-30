namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sentry;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Handler for CreateBudgetCommand.
/// Validates category existence, checks for temporal overlap via BudgetService,
/// creates the Budget aggregate, and persists it.
/// Slice 4 — Budget redesign.
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
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);
            var categoryId = new CategoryId(request.CategoryId);

            // Validate category exists and belongs to the current user
            Category? category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category is null || !category.IsAccessibleToUser(userId))
                throw new EntityNotFoundException("Category", request.CategoryId);

            // Validate no temporal overlap with existing budgets for same user + category
            await _budgetService.ValidateNoOverlap(
                userId,
                categoryId,
                request.EffectiveFrom,
                request.EffectiveUntil);

            // Create the aggregate
            var limit = new Money(request.LimitAmount);
            var budgetId = new BudgetId(Guid.NewGuid());
            var budget = new Budget(
                budgetId,
                userId,
                categoryId,
                request.EffectiveFrom,
                request.EffectiveUntil,
                request.PeriodGranularity,
                limit);

            await _budgetRepo.AddAsync(budget);
            return budgetId.Value;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "CreateBudgetCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred. Please check your connection and try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "CreateBudgetCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while creating the budget. Please try again.");
        }
    }
}

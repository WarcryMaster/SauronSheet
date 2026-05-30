namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sentry;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Handler for UpdateBudgetEffectiveDatesCommand.
/// Validates ownership, checks for temporal overlap with other budgets
/// for the same user+category, delegates to Budget.UpdateEffectiveDates
/// domain method, and persists.
/// Verify phase fix — Issue 3.
/// </summary>
public class UpdateBudgetEffectiveDatesCommandHandler : IRequestHandler<UpdateBudgetEffectiveDatesCommand>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly BudgetService _budgetService;
    private readonly IUserContext _userContext;

    public UpdateBudgetEffectiveDatesCommandHandler(
        IBudgetRepository budgetRepo,
        BudgetService budgetService,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task Handle(UpdateBudgetEffectiveDatesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);
            var budgetId = new BudgetId(request.BudgetId);

            var budget = await _budgetRepo.GetByIdAsync(budgetId);
            if (budget is null || budget.UserId.Value != userId.Value)
                throw new EntityNotFoundException("Budget", request.BudgetId);

            // Validate no temporal overlap with other budgets for the same
            // user+category before applying the new date range.
            await _budgetService.ValidateNoOverlap(
                userId,
                budget.CategoryId,
                request.EffectiveFrom,
                request.EffectiveUntil,
                excludeBudgetId: budgetId);

            budget.UpdateEffectiveDates(request.EffectiveFrom, request.EffectiveUntil);

            await _budgetRepo.UpdateAsync(budget);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "UpdateBudgetEffectiveDatesCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred while updating budget dates. Please try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "UpdateBudgetEffectiveDatesCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while updating budget dates. Please try again.");
        }
    }
}

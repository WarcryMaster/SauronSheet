namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sentry;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Handler for DeactivateBudgetCommand.
/// Validates ownership, delegates to Budget.Deactivate domain method, and persists.
/// Slice 4 — Budget redesign.
/// </summary>
public class DeactivateBudgetCommandHandler : IRequestHandler<DeactivateBudgetCommand>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly IUserContext _userContext;

    public DeactivateBudgetCommandHandler(IBudgetRepository budgetRepo, IUserContext userContext)
    {
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task Handle(DeactivateBudgetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);
            var budgetId = new BudgetId(request.BudgetId);

            var budget = await _budgetRepo.GetByIdAsync(budgetId);
            if (budget is null || budget.UserId.Value != userId.Value)
                throw new EntityNotFoundException("Budget", request.BudgetId);

            budget.Deactivate(request.AsOf);

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
                scope.SetTag("handler", "DeactivateBudgetCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred while deactivating the budget. Please try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "DeactivateBudgetCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while deactivating the budget. Please try again.");
        }
    }
}

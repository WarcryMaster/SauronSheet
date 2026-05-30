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
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Handler for DeleteBudgetCommand.
/// Validates ownership (tenant scoping), then physically deletes the budget.
/// Different from deactivate — this is a hard delete.
/// Verify phase fix — Issue 4.
/// </summary>
public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly IUserContext _userContext;

    public DeleteBudgetCommandHandler(IBudgetRepository budgetRepo, IUserContext userContext)
    {
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);
            var budgetId = new BudgetId(request.BudgetId);

            var budget = await _budgetRepo.GetByIdAsync(budgetId);
            if (budget is null || budget.UserId.Value != userId.Value)
                throw new EntityNotFoundException("Budget", request.BudgetId);

            await _budgetRepo.DeleteAsync(budgetId);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "DeleteBudgetCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred while deleting the budget. Please try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "DeleteBudgetCommandHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while deleting the budget. Please try again.");
        }
    }
}

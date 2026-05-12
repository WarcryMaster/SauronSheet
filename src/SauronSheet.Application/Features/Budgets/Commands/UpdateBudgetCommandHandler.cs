namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

/// <summary>
/// Handler for UpdateBudgetCommand.
/// Validates ownership and delegates to Budget.UpdateLimit domain method.
/// </summary>
public class UpdateBudgetCommandHandler : IRequestHandler<UpdateBudgetCommand, Unit>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly IUserContext _userContext;

    public UpdateBudgetCommandHandler(IBudgetRepository budgetRepo, IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgetId = new BudgetId(request.BudgetId);

        var budget = await _budgetRepo.GetByIdAsync(budgetId);
        if (budget is null || budget.UserId.Value != userId.Value)
            throw new EntityNotFoundException("Budget", request.BudgetId);

        var newLimit = new Money(request.NewLimitAmount);
        budget.UpdateLimit(newLimit);

        await _budgetRepo.UpdateAsync(budget);
        return Unit.Value;
    }
}

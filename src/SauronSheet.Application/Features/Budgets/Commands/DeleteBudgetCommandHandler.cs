namespace SauronSheet.Application.Features.Budgets.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

/// <summary>
/// Handler for DeleteBudgetCommand.
/// Validates ownership and deletes budget. No transaction cascade.
/// </summary>
public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand, Unit>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly IUserContext _userContext;

    public DeleteBudgetCommandHandler(IBudgetRepository budgetRepo, IUserContext userContext)
    {
        _budgetRepo = budgetRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var budgetId = new BudgetId(request.BudgetId);

        var budget = await _budgetRepo.GetByIdAsync(budgetId);
        if (budget is null || budget.UserId.Value != userId.Value)
            throw new EntityNotFoundException("Budget", request.BudgetId);

        await _budgetRepo.DeleteAsync(budgetId);
        return Unit.Value;
    }
}

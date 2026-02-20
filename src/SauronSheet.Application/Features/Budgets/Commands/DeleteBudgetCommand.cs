namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to delete a budget.
/// No cascading effects on transactions (budget is a tracking overlay).
/// Phase 5 (Scenario 5.4).
/// </summary>
public record DeleteBudgetCommand(Guid BudgetId) : IRequest<Unit>;

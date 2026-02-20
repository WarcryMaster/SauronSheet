namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to update an existing budget's spending limit.
/// Only the limit amount can be changed; category and period are immutable.
/// Phase 5 (Scenario 5.3).
/// </summary>
public record UpdateBudgetCommand(Guid BudgetId, decimal NewLimitAmount) : IRequest<Unit>;

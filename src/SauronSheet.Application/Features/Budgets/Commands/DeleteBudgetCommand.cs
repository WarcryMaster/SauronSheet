namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to physically delete a budget.
/// Different from DeactivateBudgetCommand — this removes the budget entirely,
/// not just sets an end date. Requires ownership verification for security.
/// Verify phase fix — Issue 4: physical delete was missing.
/// </summary>
public record DeleteBudgetCommand(Guid BudgetId) : IRequest;

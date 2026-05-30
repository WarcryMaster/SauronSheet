namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to update only the spending limit of an existing budget.
/// The new limit must be positive.
/// Slice 4 — Budget redesign: Application Commands.
/// </summary>
public record UpdateBudgetLimitCommand(Guid BudgetId, decimal NewLimitAmount) : IRequest;

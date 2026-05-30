namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to deactivate a budget by setting its EffectiveUntil date.
/// A deactivated budget no longer applies to dates after the given asOf date.
/// Slice 4 — Budget redesign: Application Commands.
/// </summary>
public record DeactivateBudgetCommand(Guid BudgetId, DateOnly AsOf) : IRequest;

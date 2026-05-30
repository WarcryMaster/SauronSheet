namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to update the effective date range of an existing budget policy.
/// Changes when the budget applies from and until (null = permanent).
/// Verify phase fix — Issue 3: Edit page now supports date updates.
/// </summary>
public record UpdateBudgetEffectiveDatesCommand(
    Guid BudgetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil) : IRequest;

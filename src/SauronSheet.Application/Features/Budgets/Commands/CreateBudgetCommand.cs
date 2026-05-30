namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Command to create a new budget policy with configurable granularity.
/// A budget defines a spending limit per period (Monthly, Quarterly, Semester, Annual)
/// that applies from EffectiveFrom until deactivated or modified.
/// Slice 4 — Budget redesign: Application Commands.
/// </summary>
public record CreateBudgetCommand(
    Guid CategoryId,
    decimal LimitAmount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil,
    BudgetPeriod PeriodGranularity) : IRequest<Guid>;

namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Command to update both the period granularity and the spending limit of a budget.
/// The new granularity changes how the limit is applied (e.g., from Monthly to Annual),
/// and the new limit must be positive.
/// Slice 4 — Budget redesign: Application Commands.
/// </summary>
public record UpdateBudgetPeriodCommand(
    Guid BudgetId,
    BudgetPeriod NewPeriod,
    decimal NewLimitAmount) : IRequest;

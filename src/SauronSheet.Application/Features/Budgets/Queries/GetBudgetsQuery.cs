namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get all budgets for the current user, with optional AsOf filter.
/// Returns BudgetDto list reflecting the new Budget policy model.
/// Slice 5 — Budget redesign.
/// </summary>
public record GetBudgetsQuery(DateOnly? AsOf = null) : IRequest<List<BudgetDto>>;

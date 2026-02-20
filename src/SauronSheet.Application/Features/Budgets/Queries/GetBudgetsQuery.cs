namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get all budgets for the current user, with optional year/month filter.
/// Returns BudgetStatusDto with spend calculations for progress bars and status indicators.
/// Phase 5 (Scenario 5.2).
/// </summary>
public record GetBudgetsQuery(int? Year = null, int? Month = null) : IRequest<List<BudgetStatusDto>>;

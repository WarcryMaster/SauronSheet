namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get a single budget with current spend status.
/// Phase 5 (Scenario 5.7).
/// </summary>
public record GetBudgetByIdQuery(Guid BudgetId) : IRequest<BudgetStatusDto>;

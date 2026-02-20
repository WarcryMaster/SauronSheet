namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get aggregated budget health summary for the dashboard widget.
/// Phase 5 (Scenario 5.5).
/// </summary>
public record GetBudgetSummaryForDashboardQuery(int Year, int Month)
    : IRequest<BudgetDashboardSummaryDto>;

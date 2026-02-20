namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get budget vs. actual spending comparison for a given month.
/// Phase 5 (Scenario 5.6).
/// </summary>
public record GetBudgetVsActualQuery(int Year, int Month) : IRequest<List<BudgetVsActualDto>>;

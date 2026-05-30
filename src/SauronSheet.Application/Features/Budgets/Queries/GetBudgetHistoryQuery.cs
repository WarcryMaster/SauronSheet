namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get budget history for a given year.
/// Returns per-period summaries with accumulated metrics.
/// Slice 5 — Budget redesign.
/// </summary>
public record GetBudgetHistoryQuery(int Year) : IRequest<List<BudgetPeriodSummaryDto>>;

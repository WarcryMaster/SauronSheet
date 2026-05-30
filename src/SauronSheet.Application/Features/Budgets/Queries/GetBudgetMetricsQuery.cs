namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get budget metrics for a date range.
/// Returns per-budget metrics including period calculations and spending.
/// When PerBudgetDateRanges is provided, each budget uses its own range
/// for period calculation and spending filtering, while From/To is used
/// as the global fallback for transaction fetching and budgets without
/// an explicit range.
/// Slice 5 — Budget redesign.
/// </summary>
public record GetBudgetMetricsQuery(
    DateOnly From,
    DateOnly To,
    Dictionary<Guid, BudgetDateRange>? PerBudgetDateRanges = null)
    : IRequest<List<BudgetMetricsDto>>;

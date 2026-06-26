namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get monthly spending trends for a date range.
/// Emits one entry per calendar month overlapping the range, padding missing months with zeros.
/// Phase 4 (US3).
/// </summary>
public record GetMonthlyTrendsQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<List<MonthlyTrendDto>>;

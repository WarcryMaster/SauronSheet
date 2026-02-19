namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get monthly spending trends for a year (12 entries).
/// Phase 4 (US3).
/// </summary>
public record GetMonthlyTrendsQuery(int Year) : IRequest<List<MonthlyTrendDto>>;

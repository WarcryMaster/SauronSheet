namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to compare spending between two years month-by-month.
/// Phase 4 (US4).
/// </summary>
public record GetYearlyComparisonQuery(int Year1, int Year2) : IRequest<List<YearlyComparisonDto>>;

namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get spending breakdown by category for a date range.
/// Phase 4 (US2).
/// </summary>
public record GetSpendingByCategoryQuery(DateTime FromDate, DateTime ToDate) : IRequest<List<CategorySpendingDto>>;

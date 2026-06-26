namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get monthly spending breakdown by category for stacked area charts.
/// Returns per-category amounts for each month in the specified date range.
/// Categories are sorted by total amount descending for legend ordering.
/// </summary>
public record GetMonthlySpendingByCategoryQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<List<MonthlyCategorySpendingDto>>;

namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get monthly spending breakdown by category for stacked area charts.
/// Returns per-category amounts for each month in the specified year.
/// </summary>
public record GetMonthlySpendingByCategoryQuery(int Year) : IRequest<List<MonthlyCategorySpendingDto>>;

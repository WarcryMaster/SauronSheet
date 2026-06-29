namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get the full Annual Dashboard for the given year.
/// Returns the fixed/variable analysis, executive summary, financial ratios,
/// health score, and a smart summary narrative.
/// </summary>
public record GetAnnualDashboardQuery(int Year) : IRequest<GetAnnualDashboardResultDto>;

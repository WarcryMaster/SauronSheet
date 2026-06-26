namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get the annual fixed vs variable analysis for the given year.
/// </summary>
public record GetAnnualAnalysisQuery(int Year) : IRequest<AnnualAnalysisResultDto>;
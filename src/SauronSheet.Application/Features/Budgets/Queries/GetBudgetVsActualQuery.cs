namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to compare budget vs actual spending for a date range.
/// Returns a comparison for all categories with spending activity,
/// including categories without defined budgets.
/// Slice 5 — Budget redesign.
/// </summary>
public record GetBudgetVsActualQuery(DateOnly From, DateOnly To) : IRequest<List<BudgetVsActualDto>>;

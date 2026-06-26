namespace SauronSheet.Application.Features.Budgets.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get a single budget by its ID for the current user.
/// Dedicated query to avoid loading all budgets and filtering in-memory.
/// </summary>
public record GetBudgetByIdQuery(Guid BudgetId) : IRequest<BudgetDto?>;

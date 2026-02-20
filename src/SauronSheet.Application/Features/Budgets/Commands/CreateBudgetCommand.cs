namespace SauronSheet.Application.Features.Budgets.Commands;

using MediatR;

/// <summary>
/// Command to create a new monthly budget for a category.
/// Phase 5 (Scenario 5.1).
/// </summary>
public record CreateBudgetCommand(
    Guid CategoryId,
    decimal LimitAmount,
    DateTime PeriodStart,
    DateTime PeriodEnd) : IRequest<Guid>;

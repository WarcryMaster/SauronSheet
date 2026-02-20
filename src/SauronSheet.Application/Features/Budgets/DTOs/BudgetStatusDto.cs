namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Budget with current spend status for detail views.
/// Includes calculated fields: CurrentSpend, RemainingAmount, PercentageUsed, StatusLevel.
/// </summary>
public record BudgetStatusDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal LimitAmount,
    decimal CurrentSpend,
    decimal RemainingAmount,
    decimal PercentageUsed,
    string StatusLevel,
    string Currency,
    DateTime PeriodStart,
    DateTime PeriodEnd);

namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Basic budget representation for list views.
/// </summary>
public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal LimitAmount,
    string Currency,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

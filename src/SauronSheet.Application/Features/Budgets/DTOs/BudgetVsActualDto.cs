namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Budget vs. actual comparison per category for a given month.
/// Categories with spending but no budget show BudgetLimit as null.
/// </summary>
public record BudgetVsActualDto(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal? BudgetLimit,
    decimal ActualSpend,
    decimal? Difference,
    decimal? PercentageUsed,
    string? StatusLevel,
    string Currency);

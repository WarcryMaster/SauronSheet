namespace SauronSheet.Application.Features.Budgets.DTOs;

/// <summary>
/// Redesigned budget representation for list views.
/// Reflects the new Budget entity: policy-based with configurable period granularity.
/// Slice 5 — Budget redesign.
/// </summary>
public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil,
    string PeriodGranularity,
    decimal Limit);

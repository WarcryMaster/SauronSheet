namespace SauronSheet.Application.Features.Budgets.DTOs;

using System.Collections.Generic;

/// <summary>
/// Aggregated budget health summary for the dashboard widget.
/// Shows list of budget statuses and summary counts.
/// </summary>
public record BudgetDashboardSummaryDto(
    List<BudgetStatusDto> Budgets,
    int TotalBudgets,
    int OnTrackCount,
    int OverBudgetCount);

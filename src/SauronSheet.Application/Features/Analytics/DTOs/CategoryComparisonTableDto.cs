namespace SauronSheet.Application.Features.Analytics.DTOs;

using System.Collections.Generic;

/// <summary>
/// A single row in the category comparison table (REQ-007).
/// Shows Category | Prev | Sel | Next | Δ€ | Δ% | Trend.
/// Sort by diff descending (handled by service).
/// </summary>
public record CategoryComparisonRowDto(
    string CategoryName,
    decimal PreviousYearAmount,
    decimal SelectedYearAmount,
    decimal? NextYearAmount,
    decimal DiffAbs,
    decimal DiffPct,
    string Trend);

/// <summary>
/// Category Comparison Table DTO (REQ-007).
/// Wraps rows sorted by absolute difference descending.
/// </summary>
public record CategoryComparisonTableDto(
    IReadOnlyList<CategoryComparisonRowDto> Rows);

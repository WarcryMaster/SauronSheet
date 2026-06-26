namespace SauronSheet.Application.Features.Analytics.DTOs;

using System.Collections.Generic;
using Classification;

/// <summary>
/// A single row in the annual analysis table.
/// MonthlyAmounts contains 12 entries, index 0 is January.
/// </summary>
public record AnnualAnalysisRowDto(
    string Movement,
    AnalysisLineType LineType,
    string TypeLabel,
    decimal Average,
    IReadOnlyList<decimal> MonthlyAmounts,
    string Currency);

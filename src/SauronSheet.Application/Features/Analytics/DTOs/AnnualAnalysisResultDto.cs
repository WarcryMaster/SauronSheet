namespace SauronSheet.Application.Features.Analytics.DTOs;

using System.Collections.Generic;

/// <summary>
/// Result of the annual fixed vs variable analysis for a single year.
/// </summary>
public record AnnualAnalysisResultDto(
    int Year,
    IReadOnlyList<AnnualAnalysisRowDto> Rows,
    AnnualAnalysisSummaryDto Summary,
    bool HasData,
    string Currency);
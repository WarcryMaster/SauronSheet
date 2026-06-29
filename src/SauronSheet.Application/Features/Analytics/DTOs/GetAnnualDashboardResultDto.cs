namespace SauronSheet.Application.Features.Analytics.DTOs;

using System.Collections.Generic;

/// <summary>
/// Combined result of the Annual Dashboard query.
/// Bundles the existing fixed/variable analysis with the new
/// executive summary, financial ratios, health score, smart summary,
/// and T2 multi-year/category/timeline data.
/// </summary>
public record GetAnnualDashboardResultDto(
    int Year,
    IReadOnlyList<AnnualAnalysisRowDto> Rows,
    AnnualAnalysisSummaryDto AnalysisSummary,
    AnnualDashboardSummaryDto? ExecutiveSummary,
    AnnualDashboardRatiosDto? Ratios,
    AnnualDashboardHealthScoreDto? HealthScore,
    string SmartSummary,
    bool HasData,
    string Currency,
    IReadOnlyList<int> AvailableYears,

    // T2 — Multi-Year & Categories & Timeline
    AnnualDashboardMultiYearDto? MultiYear,
    AnnualDashboardMonthlyDto? MonthlyEvolution,
    IReadOnlyList<CategoryItemDto>? Categories,
    CategoryComparisonTableDto? CategoryTable,
    IReadOnlyList<TimelineEventDto>? Timeline,
    IReadOnlyList<TopMovementDto>? TopExpenses,
    IReadOnlyList<TopMovementDto>? TopIncomes,
    IReadOnlyList<TopMovementDto>? MostFrequent,

    // T3 — Advanced
    IReadOnlyList<AnomalyDto>? Anomalies = null,
    IReadOnlyList<DiscoveryDto>? Discoveries = null,
    IReadOnlyList<AchievementDto>? Achievements = null,
    IReadOnlyList<TrendDto>? Trends = null,
    PredictionDto? Predictions = null,
    HistoricalComparisonDto? HistoricalComparison = null);

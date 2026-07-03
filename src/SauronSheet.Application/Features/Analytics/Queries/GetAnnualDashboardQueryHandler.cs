namespace SauronSheet.Application.Features.Analytics.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Classification;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using Sentry;
using SauronSheet.Domain.Common;
using Services;

/// <summary>
/// Handler for GetAnnualDashboardQuery.
/// Orchestrates all analytics services:
///   1. Classification engine (existing)
///   2. Executive summary (AnnualSummaryService)
///   3. Financial ratios (FinancialRatiosService)
///   4. Health score (HealthScoreService)
///   5. Smart summary (InsightsService)
/// Also computes year navigation context (prev/next year availability).
/// </summary>
public class GetAnnualDashboardQueryHandler
    : IRequestHandler<GetAnnualDashboardQuery, GetAnnualDashboardResultDto>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;
    private readonly IAnnualClassificationEngine _classificationEngine;
    private readonly InsightsService _insightsService;
    private readonly AnomalyDetectionService _anomalyDetectionService;

    public GetAnnualDashboardQueryHandler(
        ITransactionRepository transactionRepo,
        ISubcategoryRepository subcategoryRepo,
        IUserContext userContext,
        IAnnualClassificationEngine classificationEngine,
        InsightsService insightsService,
        AnomalyDetectionService anomalyDetectionService)
    {
        _transactionRepo = transactionRepo;
        _subcategoryRepo = subcategoryRepo;
        _userContext = userContext;
        _classificationEngine = classificationEngine;
        _insightsService = insightsService;
        _anomalyDetectionService = anomalyDetectionService;
    }

    public async Task<GetAnnualDashboardResultDto> Handle(
        GetAnnualDashboardQuery request,
        CancellationToken cancellationToken)
    {
        ISpan? parentSpan = SentrySdk.GetSpan();
        ISpan? handlerSpan = parentSpan?.StartChild(
            "analytics.handler",
            "GetAnnualDashboardQueryHandler.Handle");
        DateTime handlerStart = DateTime.UtcNow;

        try
        {
        UserId userId = new(_userContext.UserId);

        // 1. Load date range (ONCE — shared by all subsequent computations)
        (DateTime MinDate, DateTime MaxDate)? dateRange =
            await _transactionRepo.GetDateRangeAsync(userId);

        if (!dateRange.HasValue)
        {
            return BuildEmptyResult(request.Year, Array.Empty<int>());
        }

        int minYear = dateRange.Value.MinDate.Year;
        int maxYear = dateRange.Value.MaxDate.Year;

        // 2. Load subcategories once
        IReadOnlyList<Subcategory> subcategories = await _subcategoryRepo.GetByUserIdAsync(userId);
        Dictionary<SubcategoryId, string> subcategoryNames = subcategories
            .ToDictionary(s => s.Id, s => s.Name.Value);

        // 3. ONE wide-range query — load all transactions for the full range (C3 fix)
        IReadOnlyList<Transaction> allTransactions = await _transactionRepo
            .GetByUserIdAndYearRangeAsync(userId, minYear, maxYear);

        // 4. Partition by year in memory (no more per-year DB queries)
        Dictionary<int, List<Transaction>> transactionsByYear = allTransactions
            .Where(t => !t.Amount.IsZero)
            .GroupBy(t => t.Date.Year)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<int> availableYears = transactionsByYear.Keys
            .OrderBy(y => y)
            .ToList();

        int totalYears = availableYears.Count;
        int minAvailableYear = totalYears > 0 ? availableYears[0] : 0;
        int maxAvailableYear = totalYears > 0 ? availableYears[totalYears - 1] : 0;

        int selectedYear = request.Year;

        // 5. Handle empty selected year
        if (!transactionsByYear.TryGetValue(selectedYear, out List<Transaction>? currentYearTransactions)
            || currentYearTransactions.Count == 0)
        {
            return BuildEmptyResult(selectedYear, availableYears);
        }

        // 6. Classify current year
        IReadOnlyList<AnnualAnalysisRowDto> rows = _classificationEngine.Classify(
            currentYearTransactions, subcategoryNames, selectedYear);

        string currency = rows.FirstOrDefault()?.Currency ?? "EUR";

        // 7. Previous year data from partition (NO extra DB query)
        int selectedYearIndex = availableYears.IndexOf(selectedYear);
        int? prevYear = selectedYearIndex > 0
            ? availableYears[selectedYearIndex - 1]
            : null;

        decimal? prevIncome = null;
        decimal? prevExpense = null;
        decimal? prevNet = null;
        decimal? prevSavings = null;
        decimal? prevSavingsRate = null;

        if (prevYear.HasValue
            && transactionsByYear.TryGetValue(prevYear.Value, out List<Transaction>? prevYearTxs)
            && prevYearTxs.Count > 0)
        {
            IReadOnlyList<AnnualAnalysisRowDto> prevRows = _classificationEngine.Classify(
                prevYearTxs, subcategoryNames, prevYear.Value);

            decimal prevIncomeTotal = prevRows
                .Where(r => r.IsIncome)
                .Sum(r => r.MonthlyAmounts.Sum());

            decimal prevExpenseTotal = prevRows
                .Where(r => !r.IsIncome)
                .Sum(r => r.MonthlyAmounts.Sum());

            prevIncome = prevIncomeTotal;
            prevExpense = prevExpenseTotal;
            prevNet = prevIncomeTotal - prevExpenseTotal;
            prevSavings = prevIncomeTotal - prevExpenseTotal;

            if (prevIncomeTotal > 0m)
            {
                prevSavingsRate = Math.Round((prevIncomeTotal - prevExpenseTotal) / prevIncomeTotal * 100m, 2);
            }
        }

        // 8. Compute executive summary
        DateTime annualSummaryStart = DateTime.UtcNow;
        AnnualDashboardSummaryDto executiveSummary = AnnualSummaryService.Compute(
            currentYearTransactions,
            selectedYear,
            rows,
            subcategoryNames,
            minAvailableYear,
            maxAvailableYear,
            prevIncome,
            prevExpense,
            prevNet,
            prevSavings,
            prevSavingsRate);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.annual_summary_ms",
            (DateTime.UtcNow - annualSummaryStart).TotalMilliseconds);

        // 9. Year rank from partitioned data (synchronous — no DB calls, C3/C4 fix)
        int? yearRank = ComputeYearRank(transactionsByYear, selectedYear, subcategoryNames);
        executiveSummary = executiveSummary with { YearRank = yearRank };

        // 10. Compute financial ratios
        DateTime financialRatiosStart = DateTime.UtcNow;
        AnnualDashboardRatiosDto ratios = FinancialRatiosService.Compute(currentYearTransactions, selectedYear);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.financial_ratios_ms",
            (DateTime.UtcNow - financialRatiosStart).TotalMilliseconds);

        // 11. Compute health score
        DateTime healthScoreStart = DateTime.UtcNow;
        AnnualDashboardHealthScoreDto healthScore = HealthScoreService.Compute(
            currentYearTransactions, executiveSummary, ratios, rows);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.health_score_ms",
            (DateTime.UtcNow - healthScoreStart).TotalMilliseconds);

        // 12. Generate smart summary
        DateTime smartSummaryStart = DateTime.UtcNow;
        string smartSummary = _insightsService.GenerateSmartSummary(
            currentYearTransactions, executiveSummary, ratios, rows);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.smart_summary_ms",
            (DateTime.UtcNow - smartSummaryStart).TotalMilliseconds);

        // 13. Compute months with data
        int monthsWithData = currentYearTransactions
            .Select(t => t.Date.Month)
            .Distinct()
            .Count();

        // 14. Build the existing analysis result fields from classified rows
        AnnualAnalysisSummaryDto analysisSummary = BuildAnalysisSummary(rows, currency, monthsWithData);

        // 15. T2 — Multi-Year & Monthly Evolution (REQ-003, REQ-004)
        AnnualDashboardMultiYearDto? multiYear = MultiYearComparisonService.Compute(transactionsByYear, selectedYear);
        AnnualDashboardMonthlyDto monthlyEvolution = MonthlyEvolutionService.Compute(currentYearTransactions, selectedYear, transactionsByYear);

        // 16. T2 — Category Analysis (REQ-005, REQ-006, REQ-007)
        IReadOnlyList<AnnualAnalysisRowDto>? prevYearRows = null;
        if (prevYear.HasValue
            && transactionsByYear.TryGetValue(prevYear.Value, out List<Transaction>? prevYearTxsForCat)
            && prevYearTxsForCat is { Count: > 0 })
        {
            prevYearRows = _classificationEngine.Classify(prevYearTxsForCat, subcategoryNames, prevYear.Value);
        }

        IReadOnlyList<AnnualAnalysisRowDto>? nextYearRows = null;
        int? nextYear = selectedYearIndex >= 0 && selectedYearIndex < availableYears.Count - 1
            ? availableYears[selectedYearIndex + 1]
            : null;
        if (nextYear.HasValue
            && transactionsByYear.TryGetValue(nextYear.Value, out List<Transaction>? nextYearTxs)
            && nextYearTxs is { Count: > 0 })
        {
            nextYearRows = _classificationEngine.Classify(nextYearTxs, subcategoryNames, nextYear.Value);
        }

        (IReadOnlyList<CategoryItemDto> categories, CategoryComparisonTableDto? categoryTable) =
            CategoryAnalysisService.ComputeCategories(rows, prevYearRows, nextYearRows);

        // 17. T2 — Timeline & Top Movements (REQ-009, REQ-010)
        IReadOnlyList<TimelineEventDto> timeline = TimelineService.Compute(currentYearTransactions, selectedYear);
        TopMovementsResult topMovements = TopMovementsService.Compute(currentYearTransactions, selectedYear);

        // 18. Build annual summaries for all years (reused by multiple T3 services)
        Dictionary<int, AnnualDashboardSummaryDto> yearlySummaries = BuildYearlySummaries(transactionsByYear, subcategoryNames);

        // 19. T3 — Advanced (REQ-008, 013, 014, 015, 016, 017)
        DateTime anomalyStart = DateTime.UtcNow;
        IReadOnlyList<AnomalyDto> anomalies = _anomalyDetectionService.Compute(transactionsByYear, selectedYear);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.anomaly_detection_ms",
            (DateTime.UtcNow - anomalyStart).TotalMilliseconds);

        DateTime discoveriesStart = DateTime.UtcNow;
        IReadOnlyList<DiscoveryDto> discoveries = _insightsService.GenerateDiscoveries(currentYearTransactions);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.discoveries_ms",
            (DateTime.UtcNow - discoveriesStart).TotalMilliseconds);

        DateTime achievementsStart = DateTime.UtcNow;
        Dictionary<int, decimal> yearlyRestaurantExpenses = BuildYearlyRestaurantExpenses(transactionsByYear);
        IReadOnlyList<AchievementDto> achievements = AchievementsService.Compute(yearlySummaries, selectedYear, yearlyRestaurantExpenses);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.achievements_ms",
            (DateTime.UtcNow - achievementsStart).TotalMilliseconds);

        DateTime trendsStart = DateTime.UtcNow;
        IReadOnlyDictionary<string, decimal?> categoryYoYChanges = CategoryAnalysisService.BuildCategoryYoYChanges(categories);
        IReadOnlyList<TrendDto> trends = TrendDetectionService.Compute(categoryYoYChanges);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.trends_ms",
            (DateTime.UtcNow - trendsStart).TotalMilliseconds);

        DateTime predictionsStart = DateTime.UtcNow;
        PredictionDto predictions = PredictionService.Compute(yearlySummaries, selectedYear);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.predictions_ms",
            (DateTime.UtcNow - predictionsStart).TotalMilliseconds);

        DateTime historicalStart = DateTime.UtcNow;
        HistoricalComparisonDto historicalComparison = HistoricalComparisonService.Compute(yearlySummaries, selectedYear);
        SentrySdk.Experimental.Metrics.EmitDistribution(
            "app.analytics.annual_dashboard.historical_comparison_ms",
            (DateTime.UtcNow - historicalStart).TotalMilliseconds);

        // 20. Build result with all tiers
        return new GetAnnualDashboardResultDto(
            Year: selectedYear,
            Rows: rows,
            AnalysisSummary: analysisSummary,
            ExecutiveSummary: executiveSummary,
            Ratios: ratios,
            HealthScore: healthScore,
            SmartSummary: smartSummary,
            HasData: true,
            Currency: currency,
            AvailableYears: availableYears,

            // T2
            MultiYear: multiYear,
            MonthlyEvolution: monthlyEvolution,
            Categories: categories,
            CategoryTable: categoryTable,
            Timeline: timeline,
            TopExpenses: topMovements.TopExpenses,
            TopIncomes: topMovements.TopIncomes,
            MostFrequent: topMovements.MostFrequent,

            // T3
            Anomalies: anomalies,
            Discoveries: discoveries,
            Achievements: achievements,
            Trends: trends,
            Predictions: predictions,
            HistoricalComparison: historicalComparison);
        }
        finally
        {
            SentrySdk.Experimental.Metrics.EmitDistribution(
                "app.analytics.annual_dashboard.handler_ms",
                (DateTime.UtcNow - handlerStart).TotalMilliseconds);
            handlerSpan?.Finish();
        }
    }

    private static GetAnnualDashboardResultDto BuildEmptyResult(
        int selectedYear,
        IReadOnlyList<int>? availableYears = null)
    {
        IReadOnlyList<int> years = availableYears ?? Array.Empty<int>();
        int totalYears = years.Count;
        bool hasPreviousYear = years.Count > 0 && years.Any(y => y < selectedYear);
        bool hasNextYear = years.Count > 0 && years.Any(y => y > selectedYear);

        AnnualDashboardSummaryDto emptySummary = new(
            Income: 0m, Expense: 0m, Net: 0m, Savings: 0m, SavingsRate: 0m,
            Year: selectedYear,
            HasPreviousYear: hasPreviousYear,
            HasNextYear: hasNextYear,
            YearRank: null, TotalYears: totalYears,
            PreviousYearIncome: null, PreviousYearExpense: null,
            PreviousYearNet: null, PreviousYearSavings: null,
            PreviousYearSavingsRate: null,
            IncomeChangeAbs: null, IncomeChangePct: null,
            ExpenseChangeAbs: null, ExpenseChangePct: null,
            NetChangeAbs: null, NetChangePct: null,
            SavingsChangeAbs: null, SavingsChangePct: null,
            AverageIncome: null, AverageExpense: null,
            AverageNet: null, AverageSavings: null,
            AverageSavingsRate: null);

        return new GetAnnualDashboardResultDto(
            Year: selectedYear,
            Rows: Array.Empty<AnnualAnalysisRowDto>(),
            AnalysisSummary: new AnnualAnalysisSummaryDto(
                0m, 0m, 0m, 0m, 0m, 0m, 0m, "EUR"),
            ExecutiveSummary: emptySummary,
            Ratios: null,
            HealthScore: null,
            SmartSummary: "No data for this year. Add transactions to see the annual summary.",
            HasData: false,
            Currency: "EUR",
            AvailableYears: years,

            // T2 — empty
            MultiYear: null,
            MonthlyEvolution: null,
            Categories: null,
            CategoryTable: null,
            Timeline: null,
            TopExpenses: null,
            TopIncomes: null,
            MostFrequent: null,

            // T3 — empty
            Anomalies: null,
            Discoveries: null,
            Achievements: null,
            Trends: null,
            Predictions: null,
            HistoricalComparison: null);
    }

    private Dictionary<int, AnnualDashboardSummaryDto> BuildYearlySummaries(
        Dictionary<int, List<Transaction>> transactionsByYear,
        Dictionary<SubcategoryId, string> subcategoryNames)
    {
        Dictionary<int, AnnualDashboardSummaryDto> summaries = new();

        int minYear = transactionsByYear.Keys.Min();
        int maxYear = transactionsByYear.Keys.Max();
        int totalYears = maxYear - minYear + 1;

        foreach (KeyValuePair<int, List<Transaction>> kvp in transactionsByYear)
        {
            int year = kvp.Key;
            List<Transaction> yearTransactions = kvp.Value;

            IReadOnlyList<AnnualAnalysisRowDto> yearRows = _classificationEngine.Classify(yearTransactions, subcategoryNames, year);
            AnnualDashboardSummaryDto summary = AnnualSummaryService.Compute(
                transactions: yearTransactions,
                year: year,
                classifiedRows: yearRows,
                subcategoryNames: subcategoryNames,
                minAvailableYear: minYear,
                maxAvailableYear: maxYear,
                previousYearIncome: null,
                previousYearExpense: null,
                previousYearNet: null,
                previousYearSavings: null,
                previousYearSavingsRate: null) with { TotalYears = totalYears };

            summaries[year] = summary;
        }

        return summaries;
    }

    private static Dictionary<int, decimal> BuildYearlyRestaurantExpenses(Dictionary<int, List<Transaction>> transactionsByYear)
    {
        Dictionary<int, decimal> yearly = new();

        foreach (KeyValuePair<int, List<Transaction>> kvp in transactionsByYear)
        {
            decimal amount = kvp.Value
                .Where(t => t.Amount.IsNegative && !t.Amount.IsZero)
                .Where(t =>
                    (!string.IsNullOrWhiteSpace(t.BankCategory)
                     && t.BankCategory.Contains("restaurant", StringComparison.OrdinalIgnoreCase))
                    || t.Description.Contains("restaurant", StringComparison.OrdinalIgnoreCase)
                    || t.Description.Contains("restaurante", StringComparison.OrdinalIgnoreCase))
                .Sum(t => Math.Abs(t.Amount.Amount));

            yearly[kvp.Key] = amount;
        }

        return yearly;
    }

    /// <summary>
    /// Computes the year rank (net savings ranking among all years).
    /// Rank 1 = best year (highest net savings).
    /// Uses the already-loaded partitioned data — no DB queries.
    /// </summary>
    private int? ComputeYearRank(
        Dictionary<int, List<Transaction>> transactionsByYear,
        int selectedYear,
        Dictionary<SubcategoryId, string> subcategoryNames)
    {
        if (transactionsByYear.Count <= 1)
            return 1;

        Dictionary<int, decimal> yearNets = new();

        foreach (KeyValuePair<int, List<Transaction>> kvp in transactionsByYear)
        {
            IReadOnlyList<AnnualAnalysisRowDto> yearRows = _classificationEngine.Classify(
                kvp.Value, subcategoryNames, kvp.Key);

            decimal income = yearRows.Where(r => r.IsIncome).Sum(r => r.MonthlyAmounts.Sum());
            decimal expense = yearRows.Where(r => !r.IsIncome).Sum(r => r.MonthlyAmounts.Sum());
            yearNets[kvp.Key] = income - expense;
        }

        if (yearNets.Count == 0)
            return null;

        // Rank by net (descending)
        List<KeyValuePair<int, decimal>> sorted = yearNets
            .OrderByDescending(kv => kv.Value)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Key == selectedYear)
                return i + 1;
        }

        return null;
    }

    /// <summary>
    /// Builds the AnnualAnalysisSummaryDto from classified rows — mirroring
    /// the existing GetAnnualAnalysisQueryHandler logic.
    /// </summary>
    private static AnnualAnalysisSummaryDto BuildAnalysisSummary(
        IReadOnlyList<AnnualAnalysisRowDto> rows, string currency, int monthsWithData)
    {
        decimal incomeFixed = rows
            .Where(r => r.LineType == AnalysisLineType.IncomeFixed)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal incomeVariable = rows
            .Where(r => r.LineType == AnalysisLineType.IncomeVariable)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal expenseFixed = rows
            .Where(r => r.LineType == AnalysisLineType.ExpenseFixed)
            .Sum(r => r.MonthlyAmounts.Sum());

        decimal expenseVariable = rows
            .Where(r => r.LineType == AnalysisLineType.ExpenseVariable)
            .Sum(r => r.MonthlyAmounts.Sum());

        return new AnnualAnalysisSummaryDto(
            incomeFixed, incomeVariable, incomeFixed + incomeVariable,
            expenseFixed, expenseVariable, expenseFixed + expenseVariable,
            incomeFixed + incomeVariable - expenseFixed - expenseVariable,
            currency)
        {
            MonthsWithData = monthsWithData
        };
    }
}

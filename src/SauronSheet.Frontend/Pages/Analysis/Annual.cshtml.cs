using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Analysis;

/// <summary>
/// Annual Dashboard page model (PR 1 — Annual Report Redesign).
/// Combines fixed/variable analysis with executive summary, ratios, health score,
/// and a smart summary narrative.
/// </summary>
[Authorize]
public class AnnualModel : PageModel
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Selected year for the dashboard. Defaults to the current UTC year.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    /// <summary>
    /// Full dashboard result from the query handler.
    /// </summary>
    public GetAnnualDashboardResultDto? Result { get; set; }

    /// <summary>
    /// Convenience flag indicating whether the analysis contains data.
    /// </summary>
    public bool HasData => Result?.HasData ?? false;

    /// <summary>
    /// Years that contain transaction data, for year navigation (REQ-018).
    /// </summary>
    public IReadOnlyList<int> AvailableYears =>
        Result?.AvailableYears ?? Array.Empty<int>();

    /// <summary>
    /// Year navigation: whether a previous year with data exists.
    /// </summary>
    public bool HasPreviousYear => Result?.ExecutiveSummary?.HasPreviousYear ?? false;

    /// <summary>
    /// Year navigation: whether a next year with data exists.
    /// </summary>
    public bool HasNextYear => Result?.ExecutiveSummary?.HasNextYear ?? false;

    // ── T2: Multi-Year Comparison (REQ-003) ──

    /// <summary>
    /// Whether multi-year data (2+ years) is available for the bar chart.
    /// </summary>
    public bool HasMultiYear => Result?.MultiYear is not null;

    /// <summary>
    /// JSON for the multi-year comparison bar chart (REQ-003).
    /// </summary>
    public string MultiYearChartJson
    {
        get
        {
            if (Result?.MultiYear is null)
            {
                return "{}";
            }

            AnnualDashboardMultiYearDto multiYear = Result.MultiYear;

            return JsonSerializer.Serialize(new
            {
                labels = multiYear.Years.Select(y => y.ToString(CultureInfo.InvariantCulture)),
                income = multiYear.Incomes,
                expenses = multiYear.Expenses,
                savings = multiYear.Savings,
                balances = multiYear.Balances,
                highlightYear = multiYear.HighlightYear
            });
        }
    }

    // ── T2: Monthly Evolution (REQ-004) ──

    /// <summary>
    /// JSON for the monthly evolution chart (12 months + overlay of averages).
    /// </summary>
    public string MonthlyEvolutionChartJson
    {
        get
        {
            AnnualDashboardMonthlyDto? monthly = Result?.MonthlyEvolution;
            if (monthly is null)
            {
                return "{}";
            }

            return JsonSerializer.Serialize(new
            {
                labels = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" },
                income = monthly.Incomes,
                expenses = monthly.Expenses,
                savings = monthly.Savings,
                avgIncome = monthly.HistoricalAverageIncome ?? monthly.PreviousYearAverageIncome,
                avgExpense = monthly.HistoricalAverageExpense ?? monthly.PreviousYearAverageExpense,
                bestIncomeMonth = monthly.BestIncomeMonth,
                bestExpenseMonth = monthly.BestExpenseMonth,
                worstIncomeMonth = monthly.WorstIncomeMonth,
                worstExpenseMonth = monthly.WorstExpenseMonth
            });
        }
    }

    // ── T2: Category Analysis (REQ-005, REQ-006) ──

    /// <summary>
    /// JSON for the category donut chart.
    /// </summary>
    public string CategoryChartJson
    {
        get
        {
            IReadOnlyList<CategoryItemDto>? categories = Result?.Categories;
            if (categories is null || categories.Count == 0)
            {
                return "{}";
            }

            return JsonSerializer.Serialize(new
            {
                labels = categories.Select(c => c.CategoryName),
                values = categories.Select(c => c.Amount),
                percentages = categories.Select(c => c.Percentage),
                total = categories.Sum(c => c.Amount)
            });
        }
    }

    // ── T2: Category Comparison Table (REQ-007) ──

    /// <summary>
    /// Rows for the category comparison table (server-rendered, not JSON).
    /// Direct access to CategoryTable data.
    /// </summary>
    public CategoryComparisonTableDto? CategoryTable => Result?.CategoryTable;

    /// <summary>
    /// Whether we have sufficient multi-year data for the comparison table.
    /// </summary>
    public bool HasCategoryComparison => CategoryTable?.Rows is { Count: > 0 };

    // ── T2: Timeline (REQ-009) ──

    /// <summary>
    /// Timeline events as JSON for client-side rendering.
    /// </summary>
    public string TimelineJson
    {
        get
        {
            IReadOnlyList<TimelineEventDto>? timeline = Result?.Timeline;
            if (timeline is null || timeline.Count == 0)
            {
                return "[]";
            }

            return JsonSerializer.Serialize(timeline.Select(t => new
            {
                type = t.Type,
                label = t.Label,
                description = t.Description,
                date = t.Date,
                amount = t.Amount,
                icon = t.Icon
            }));
        }
    }

    // ── T2: Top Movements (REQ-010) ──

    /// <summary>
    /// JSON for top expenses list data.
    /// </summary>
    public string TopExpensesJson => SerializeTopMovements(Result?.TopExpenses);

    /// <summary>
    /// JSON for top incomes list data.
    /// </summary>
    public string TopIncomesJson => SerializeTopMovements(Result?.TopIncomes);

    /// <summary>
    /// JSON for most frequent movements list data.
    /// </summary>
    public string MostFrequentJson => SerializeTopMovements(Result?.MostFrequent);

    // ── T3: Advanced Sections (REQ-008, 013, 014, 015, 016, 017) ──

    public IReadOnlyList<AnomalyDto> Anomalies => Result?.Anomalies ?? Array.Empty<AnomalyDto>();

    public IReadOnlyList<DiscoveryDto> Discoveries => Result?.Discoveries ?? Array.Empty<DiscoveryDto>();

    public IReadOnlyList<AchievementDto> Achievements => Result?.Achievements ?? Array.Empty<AchievementDto>();

    public IReadOnlyList<TrendDto> Trends => Result?.Trends ?? Array.Empty<TrendDto>();

    public PredictionDto? Predictions => Result?.Predictions;

    public HistoricalComparisonDto? HistoricalComparison => Result?.HistoricalComparison;

    public bool HasPredictions => Predictions?.HasEnoughData ?? false;

    public string AnomalyChartJson
    {
        get
        {
            if (Anomalies.Count == 0)
            {
                return "{}";
            }

            return JsonSerializer.Serialize(new
            {
                labels = Anomalies.Select(a => $"{a.Category} ({a.Month})"),
                values = Anomalies.Select(a => a.Amount),
                means = Anomalies.Select(a => a.Mean),
                types = Anomalies.Select(a => a.Type)
            });
        }
    }

    /// <summary>
    /// Serializes a list of TopMovementDto to JSON.
    /// </summary>
    private static string SerializeTopMovements(IReadOnlyList<TopMovementDto>? movements)
    {
        if (movements is null || movements.Count == 0)
        {
            return "[]";
        }

        return JsonSerializer.Serialize(movements.Select(m => new
        {
            description = m.Description,
            amount = m.Amount,
            date = m.Date,
            type = m.Type,
            category = m.Category
        }));
    }

    /// <summary>
    /// Rows filtered to income only (IsIncome == true).
    /// </summary>
    public IReadOnlyList<AnnualAnalysisRowDto> IncomeRows =>
        Result?.Rows.Where(r => r.IsIncome).ToArray()
        ?? Array.Empty<AnnualAnalysisRowDto>();

    /// <summary>
    /// Rows filtered to expenses only (IsIncome == false).
    /// </summary>
    public IReadOnlyList<AnnualAnalysisRowDto> ExpenseRows =>
        Result?.Rows.Where(r => !r.IsIncome).ToArray()
        ?? Array.Empty<AnnualAnalysisRowDto>();

    /// <summary>
    /// Monthly aggregates across all income rows (12 entries, index 0 = January).
    /// </summary>
    public decimal[] MonthlyIncomeTotals => HasData
        ? Enumerable.Range(0, 12)
            .Select(month => Result!.Rows
                .Where(r => r.IsIncome)
                .Sum(r => r.MonthlyAmounts[month]))
            .ToArray()
        : new decimal[12];

    /// <summary>
    /// Monthly aggregates across all expense rows (12 entries, index 0 = January).
    /// </summary>
    public decimal[] MonthlyExpenseTotals => HasData
        ? Enumerable.Range(0, 12)
            .Select(month => Result!.Rows
                .Where(r => !r.IsIncome)
                .Sum(r => r.MonthlyAmounts[month]))
            .ToArray()
        : new decimal[12];

    /// <summary>
    /// JSON for the monthly trend line chart.
    /// </summary>
    public string ChartDataJson => HasData
        ? JsonSerializer.Serialize(new
        {
            labels = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" },
            income = MonthlyIncomeTotals,
            expense = MonthlyExpenseTotals
        })
        : "{}";

    /// <summary>
    /// JSON for the fixed/variable distribution donut chart.
    /// </summary>
    public string FixedVariableChartJson => HasData
        ? JsonSerializer.Serialize(new
        {
            labels = new[] { "Ingreso Fijo", "Ingreso Variable", "Gasto Fijo", "Gasto Variable" },
            values = new[]
            {
                Result!.AnalysisSummary.IncomeFixed,
                Result!.AnalysisSummary.IncomeVariable,
                Result!.AnalysisSummary.ExpenseFixed,
                Result!.AnalysisSummary.ExpenseVariable
            }
        })
        : "{}";

    /// <summary>
    /// Fixed cost percentage with zero guard.
    /// </summary>
    public decimal FixedCostPercentage => HasData && Result!.AnalysisSummary.ExpenseTotal > 0
        ? Math.Round(Result.AnalysisSummary.ExpenseFixed / Result.AnalysisSummary.ExpenseTotal * 100m, 1)
        : 0m;

    /// <summary>
    /// User-facing error message for domain rule violations.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    public AnnualModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        int selectedYear = Year ?? DateTime.UtcNow.Year;

        try
        {
            Result = await _mediator.Send(
                new GetAnnualDashboardQuery(selectedYear),
                cancellationToken);

            return Page();
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage("/Error");
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("analysis", "annual-dashboard");
                scope.SetTag("year", selectedYear.ToString(CultureInfo.InvariantCulture));
                scope.Level = Sentry.SentryLevel.Error;
            });

            return RedirectToPage("/Error");
        }
    }

    /// <summary>
    /// Formats a YoY variation percentage for display.
    /// Returns "N/A" when null, or "+XX.X%"/"-XX.X%" otherwise.
    /// </summary>
    public static string FormatVariationPct(decimal? pct)
    {
        if (pct is null)
        {
            return "N/A";
        }

        string sign = pct.Value >= 0 ? "+" : string.Empty;
        return $"{sign}{pct.Value.ToString("F1", CultureInfo.InvariantCulture)}%";
    }

    /// <summary>
    /// Returns the MDB badge class for the variation direction.
    /// Income: up=success (green), down=danger (red)
    /// Expense: up=danger (red), down=success (green)
    /// Net: up=success (green), down=danger (red)
    /// </summary>
    public static string GetVariationBadgeClass(decimal? pct, bool isIncomeOrNet)
    {
        if (pct is null || pct.Value == 0m)
        {
            return "bg-secondary";
        }

        if (isIncomeOrNet)
        {
            return pct.Value > 0 ? "bg-success" : "bg-danger";
        }

        // Expense: up is bad (red), down is good (green)
        return pct.Value > 0 ? "bg-danger" : "bg-success";
    }

    /// <summary>
    /// Returns the arrow symbol for the variation.
    /// Positive → up arrow, Negative → down arrow.
    /// </summary>
    public static string GetVariationArrow(decimal? pct)
    {
        if (pct is null || pct.Value == 0m)
        {
            return string.Empty;
        }

        return pct.Value > 0 ? "\u2191" : "\u2193";
    }
}

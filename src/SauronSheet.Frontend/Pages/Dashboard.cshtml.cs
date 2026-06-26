using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Queries;

namespace SauronSheet.Frontend.Pages;

/// <summary>
/// Dashboard page model with analytics data.
/// Phase 4: Full analytics dashboard with summary cards, charts and recent transactions.
/// </summary>
[Authorize]
public class DashboardModel : PageModel
{
    private readonly IMediator _mediator;

    public TransactionSummaryDto? Summary { get; set; }
    public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
    public List<YearlyComparisonDto> YearlyComparison { get; set; } = new();
    public List<MonthlyCategorySpendingDto> MonthlySpendingByCategory { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
    /// <summary>Budget metrics for the current month (widget data source).</summary>
    public List<BudgetMetricsDto> BudgetMetrics { get; set; } = new();

    /// <summary>Total percentage consumed across all budgets in current month.</summary>
    public decimal BudgetTotalPercentageUsed { get; set; }

    /// <summary>Count of budgets by status level for the widget summary.</summary>
    public int BudgetGreenCount { get; set; }
    public int BudgetYellowCount { get; set; }
    public int BudgetRedCount { get; set; }
    public int BudgetOverageCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string DateFilter { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomFromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomToDate { get; set; }

    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>Human-readable date range label for chart titles (e.g., "Jan 2025 – Jun 2026").</summary>
    public string ChartDateRangeLabel { get; set; } = "";

    /// <summary>Year 1 for YoY comparison (ToDate.Year - 1).</summary>
    public int YoYYear1 => ToDate.Year - 1;

    /// <summary>Year 2 for YoY comparison (ToDate.Year).</summary>
    public int YoYYear2 => ToDate.Year;

    public DashboardModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            CalculateDateRange();
            ChartDateRangeLabel = BuildDateRangeLabel();

            Summary = await _mediator.Send(new GetTransactionSummaryQuery(FromDate, ToDate));
            MonthlyTrends = await _mediator.Send(new GetMonthlyTrendsQuery(FromDate, ToDate));
            YearlyComparison = await _mediator.Send(new GetYearlyComparisonQuery(YoYYear1, YoYYear2));
            MonthlySpendingByCategory = await _mediator.Send(new GetMonthlySpendingByCategoryQuery(FromDate, ToDate));
            RecentTransactions = await _mediator.Send(new GetRecentTransactionsQuery(10));

            // Budget status widget — use GetBudgetMetricsQuery for current month
            var nowDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var monthFrom = new DateOnly(nowDate.Year, nowDate.Month, 1);
            var monthTo = monthFrom.AddMonths(1).AddDays(-1);

            var allMetrics = await _mediator.Send(new GetBudgetMetricsQuery(monthFrom, monthTo));

            // Filter out "Sin presupuesto" entries — dashboard widget only shows actual budgets.
            // Categories with spending but no budget are noise for the widget empty-state logic.
            BudgetMetrics = allMetrics
                .Where(m => m.BudgetId != Guid.Empty)
                .ToList();

            // Compute widget summary counts
            BudgetGreenCount = BudgetMetrics.Count(m => m.StatusLevel == "Green");
            BudgetYellowCount = BudgetMetrics.Count(m => m.StatusLevel == "Yellow");
            BudgetRedCount = BudgetMetrics.Count(m => m.StatusLevel == "Red");
            BudgetOverageCount = BudgetMetrics.Count(m => m.StatusLevel == "Overage");

            // Compute overall percentage consumed (weighted average)
            var totalLimit = BudgetMetrics.Sum(m => m.AccumulatedLimit);
            var totalSpent = BudgetMetrics.Sum(m => m.Spent);
            BudgetTotalPercentageUsed = totalLimit > 0
                ? Math.Round(totalSpent / totalLimit * 100, 1)
                : 0;

            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("dashboard", "OnGetAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            // Redirigir a página de error genérica
            return RedirectToPage("/Error");
        }
    }

    private void CalculateDateRange()
    {
        var now = DateTime.UtcNow;
        (FromDate, ToDate) = DateFilter switch
        {
            "all" => (DateTime.MinValue, now.Date),
            "last-month" => (new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                             new DateTime(now.Year, now.Month, 1).AddDays(-1)),
            "this-month" => (new DateTime(now.Year, now.Month, 1), now.Date),
            "this-year" => (new DateTime(now.Year, 1, 1), now.Date),
            "custom" when CustomFromDate.HasValue && CustomToDate.HasValue
                => (CustomFromDate.Value, CustomToDate.Value),
            _ => (now.AddMonths(-3).Date, now.Date) // last-3-months fallback
        };
    }

    private string BuildDateRangeLabel()
    {
        if (FromDate == DateTime.MinValue)
        {
            return $"Through {ToDate:MMM yyyy}";
        }

        if (FromDate.Year == ToDate.Year && FromDate.Month == ToDate.Month)
        {
            return $"{FromDate:MMM yyyy}";
        }

        if (FromDate.Year == ToDate.Year)
        {
            return $"{FromDate:MMM} – {ToDate:MMM yyyy}";
        }

        return $"{FromDate:MMM yyyy} – {ToDate:MMM yyyy}";
    }
}

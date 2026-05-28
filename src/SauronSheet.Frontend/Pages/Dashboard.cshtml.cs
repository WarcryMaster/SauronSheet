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
    public List<CategorySpendingDto> SpendingByCategory { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
    public List<YearlyComparisonDto> YearlyComparison { get; set; } = new();
    public List<MonthlyCategorySpendingDto> MonthlySpendingByCategory { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
    public BudgetDashboardSummaryDto? BudgetSummary { get; set; }

    [BindProperty(SupportsGet = true)]
    public string DateFilter { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomFromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? CustomToDate { get; set; }

    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int AnalyticsYear { get; set; }

    public DashboardModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            CalculateDateRange();

            Summary = await _mediator.Send(new GetTransactionSummaryQuery(FromDate, ToDate));
            AnalyticsYear = await ResolveAnalyticsYearAsync(Summary);
            SpendingByCategory = await _mediator.Send(new GetSpendingByCategoryQuery(FromDate, ToDate));
            MonthlyTrends = await _mediator.Send(new GetMonthlyTrendsQuery(AnalyticsYear));
            YearlyComparison = await _mediator.Send(new GetYearlyComparisonQuery(AnalyticsYear - 1, AnalyticsYear));
            MonthlySpendingByCategory = await _mediator.Send(new GetMonthlySpendingByCategoryQuery(AnalyticsYear));
            RecentTransactions = await _mediator.Send(new GetRecentTransactionsQuery(10));

            // Phase 5: Budget status widget
            // Budget widget always shows current month status regardless of date filter
            var now = DateTime.UtcNow;
            BudgetSummary = await _mediator.Send(new GetBudgetSummaryForDashboardQuery(now.Year, now.Month));

            return Page();
        }
        catch (UnauthorizedAccessException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("dashboard", "OnGetAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
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

    private async Task<int> ResolveAnalyticsYearAsync(TransactionSummaryDto? summary)
    {
        var currentYear = DateTime.UtcNow.Year;
        var shouldUseActualRange = DateFilter == "all" || summary?.TransactionCount == 0;

        if (!shouldUseActualRange)
        {
            return currentYear;
        }

        var actualDateRange = await _mediator.Send(new GetTransactionDateRangeQuery());
        return actualDateRange?.MaxDate.Year ?? currentYear;
    }
}

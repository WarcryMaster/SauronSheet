using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Frontend.Pages.Budgets;

/// <summary>
/// Budget metrics page with three views: current month, current period, current year.
/// The "Period" view calculates each budget's current period based on its
/// PeriodGranularity (Monthly, Quarterly, Semester, Annual) so that a
/// monthly-budget user sees their current month and an annual-budget user
/// sees their current year.
/// Slice 7 — Budget redesign.
/// </summary>
[Authorize]
public class MetricsModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetMetricsDto> Metrics { get; set; } = new();

    /// <summary>Active view: Month | Period | Year</summary>
    [BindProperty(SupportsGet = true)]
    public string View { get; set; } = "Month";

    /// <summary>Total accumulated limit across all budgets for the current view.</summary>
    public decimal TotalAccumulatedLimit { get; set; }

    /// <summary>Total spent across all budgets for the current view.</summary>
    public decimal TotalSpent { get; set; }

    /// <summary>Total remaining across all budgets for the current view.</summary>
    public decimal TotalRemaining { get; set; }

    /// <summary>Overall percentage used for the current view.</summary>
    public decimal OverallPercentageUsed { get; set; }

    public MetricsModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            DateOnly from, to;
            Dictionary<Guid, BudgetDateRange>? perBudgetRanges = null;
            var now = DateOnly.FromDateTime(DateTime.UtcNow);

            switch (View)
            {
                case "Period":
                    // "Current period" per-budget: each budget uses its own
                    // granularity to define the current period window.
                    // Global range (from/to) spans the current year so the
                    // transaction query covers all possible periods.
                    from = new DateOnly(now.Year, 1, 1);
                    to = new DateOnly(now.Year, 12, 31);

                    List<BudgetDto> allBudgets = await _mediator.Send(
                        new GetBudgetsQuery());

                    perBudgetRanges = new Dictionary<Guid, BudgetDateRange>();
                    foreach (BudgetDto budget in allBudgets)
                    {
                        if (Enum.TryParse(budget.PeriodGranularity, out BudgetPeriod parsedPeriod))
                        {
                            (DateOnly periodFrom, DateOnly periodTo) =
                                BudgetCalculationService.GetCurrentPeriodRange(parsedPeriod, now);

                            perBudgetRanges[budget.Id] = new BudgetDateRange(
                                periodFrom, periodTo);
                        }
                    }
                    break;

                case "Year":
                    from = new DateOnly(now.Year, 1, 1);
                    to = new DateOnly(now.Year, 12, 31);
                    break;

                case "Month":
                default:
                    from = new DateOnly(now.Year, now.Month, 1);
                    to = from.AddMonths(1).AddDays(-1);
                    break;
            }

            Metrics = await _mediator.Send(
                new GetBudgetMetricsQuery(from, to, perBudgetRanges));

            TotalAccumulatedLimit = Metrics.Sum(m => m.AccumulatedLimit);
            TotalSpent = Metrics.Sum(m => m.Spent);
            TotalRemaining = Metrics.Sum(m => m.Remaining);
            OverallPercentageUsed = TotalAccumulatedLimit > 0
                ? Math.Round(TotalSpent / TotalAccumulatedLimit * 100, 1)
                : 0;

            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Metrics.OnGetAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            return RedirectToPage("/Error");
        }
    }
}

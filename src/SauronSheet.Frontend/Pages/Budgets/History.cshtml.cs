using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;

namespace SauronSheet.Frontend.Pages.Budgets;

/// <summary>
/// Budget history page — shows monthly summaries for a selected year.
/// Uses GetBudgetHistoryQuery to retrieve per-period aggregated data.
/// Slice 7 — Budget redesign.
/// </summary>
[Authorize]
public class HistoryModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetPeriodSummaryDto> History { get; set; } = new();

    /// <summary>Selected year for the history view.</summary>
    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    public decimal TotalAccumulatedLimit { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }

    public HistoryModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            History = await _mediator.Send(new GetBudgetHistoryQuery(Year));

            TotalAccumulatedLimit = History.Sum(h => h.AccumulatedLimit);
            TotalSpent = History.Sum(h => h.Spent);
            TotalRemaining = History.Sum(h => h.Remaining);

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
                scope.SetTag("page", "Budgets/History.OnGetAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            return RedirectToPage("/Error");
        }
    }
}

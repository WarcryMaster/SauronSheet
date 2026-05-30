using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Budgets;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetStatusDto> Budgets { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Budget ID to delete — bound from form POST.
    /// </summary>
    [BindProperty]
    public Guid BudgetId { get; set; }

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Budgets = await _mediator.Send(new GetBudgetsQuery(Year, Month));
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            await _mediator.Send(new DeleteBudgetCommand(BudgetId));
            TempData["SuccessMessage"] = "Budget deleted successfully.";
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Budgets/Index.OnPostDeleteAsync");
                scope.SetTag("exception_type", "DomainException");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Budgets/Index.OnPostDeleteAsync");
                scope.SetTag("exception_type", "HttpRequestException");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Budgets/Index.OnPostDeleteAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred while deleting the budget. Please try again later.";
        }

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            Budgets = await _mediator.Send(new GetBudgetsQuery(Year, Month));
            return Page();
        }

        return RedirectToPage("/budgets", new { Year, Month });
    }
}

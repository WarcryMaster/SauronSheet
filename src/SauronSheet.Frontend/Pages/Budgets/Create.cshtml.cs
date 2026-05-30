using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Budgets;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty]
    public Guid CategoryId { get; set; }

    [BindProperty]
    public decimal LimitAmount { get; set; }

    /// <summary>
    /// Month string in YYYY-MM format (as sent by input[type="month"]).
    /// Converted to DateTime in OnPostAsync.
    /// </summary>
    [BindProperty]
    public string? Month { get; set; } = $"{DateTime.UtcNow.Year:D4}-{DateTime.UtcNow.Month:D2}";

    public string? ErrorMessage { get; set; }

    public CreateModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Categories = await _mediator.Send(new GetCategoriesQuery());
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Categories = await _mediator.Send(new GetCategoriesQuery());

            // Parse YYYY-MM format from input[type="month"]
            if (string.IsNullOrWhiteSpace(Month) || !DateTime.TryParse(Month + "-01", out var monthDate))
            {
                ErrorMessage = "Invalid month format. Please select a valid month.";
                Categories = await _mediator.Send(new GetCategoriesQuery());
                return Page();
            }

            var periodStart = new DateTime(monthDate.Year, monthDate.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            await _mediator.Send(new CreateBudgetCommand(
                CategoryId,
                LimitAmount,
                periodStart,
                periodEnd));

            TempData["SuccessMessage"] = "Budget created successfully.";
            return RedirectToPage("/budgets/index");
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Budgets/Create.OnPostAsync");
                scope.SetTag("exception_type", "DomainException");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
            Categories = await _mediator.Send(new GetCategoriesQuery());
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Budgets/Create.OnPostAsync");
                scope.SetTag("exception_type", "HttpRequestException");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            Categories = await _mediator.Send(new GetCategoriesQuery());
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Budgets/Create.OnPostAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred while creating the budget. Please try again later.";
            Categories = await _mediator.Send(new GetCategoriesQuery());
            return Page();
        }
    }
}

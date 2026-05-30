using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Budgets;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IReadOnlyList<BudgetDto> Budgets { get; set; } = Array.Empty<BudgetDto>();
    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool ShowActiveOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryFilter { get; set; }

    [BindProperty]
    public Guid BudgetId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            IReadOnlyList<BudgetDto> allBudgets = await _mediator.Send(new GetBudgetsQuery());
            Categories = await _mediator.Send(new GetCategoriesQuery());

            // Apply filters
            IEnumerable<BudgetDto> filtered = allBudgets;

            if (ShowActiveOnly)
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
                filtered = filtered.Where(b =>
                    b.EffectiveUntil == null || b.EffectiveUntil.Value >= today);
            }

            if (CategoryFilter.HasValue)
            {
                filtered = filtered.Where(b => b.CategoryId == CategoryFilter.Value);
            }

            Budgets = filtered.ToList();
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
    }

        public async Task<IActionResult> OnPostDeactivateAsync()
        {
            try
            {
                // Set EffectiveUntil to yesterday so the budget is immediately inactive today.
                // isActive check uses ">= today", so "yesterday" evaluates to inactive.
                var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
                await _mediator.Send(new DeactivateBudgetCommand(BudgetId, yesterday));
            TempData["SuccessMessage"] = "Budget deactivated successfully.";
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Index.OnPostDeactivateAsync");
                scope.SetTag("exception_type", "DomainException");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Index.OnPostDeactivateAsync");
                scope.SetTag("exception_type", "HttpRequestException");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Index.OnPostDeactivateAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred while deactivating the budget. Please try again later.";
        }

        return RedirectToPage("/Budgets/Index", new { ShowActiveOnly, CategoryFilter });
    }
}

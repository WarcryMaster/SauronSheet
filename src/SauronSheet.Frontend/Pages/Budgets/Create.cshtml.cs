using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Frontend.Pages.Budgets;

[Authorize]
[ValidateAntiForgeryToken]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty]
    public string BudgetType { get; set; } = string.Empty;

    [BindProperty]
    public Guid CategoryId { get; set; }

    [BindProperty]
    public decimal LimitAmount { get; set; }

    [BindProperty]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [BindProperty]
    public DateOnly? EffectiveUntil { get; set; }

    [BindProperty]
    public string PeriodGranularity { get; set; } = nameof(BudgetPeriod.Monthly);

    public string? ErrorMessage { get; set; }

    private bool IsExpenseBudget => string.Equals(BudgetType, "Expense", StringComparison.OrdinalIgnoreCase);
    private bool IsIncomeBudget => string.Equals(BudgetType, "Income", StringComparison.OrdinalIgnoreCase);

    private string LimitLabel => IsIncomeBudget ? "Income target" : "Spending limit";

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

            // Validate BudgetType
            if (string.IsNullOrWhiteSpace(BudgetType))
            {
                ErrorMessage = "Please select whether this budget is for income or expenses.";
                return Page();
            }

            if (!IsIncomeBudget && !IsExpenseBudget)
            {
                ErrorMessage = "Invalid budget type. Select either Income or Expense.";
                return Page();
            }

            // Validate Granularity
            if (!Enum.TryParse<BudgetPeriod>(PeriodGranularity, ignoreCase: true, out BudgetPeriod granularity))
            {
                ErrorMessage = "Invalid period granularity. Please select a valid option.";
                return Page();
            }

            // Validate limit is positive
            if (LimitAmount <= 0)
            {
                ErrorMessage = $"{LimitLabel} must be greater than zero.";
                return Page();
            }

            // Validate effective dates
            if (EffectiveUntil.HasValue && EffectiveUntil.Value < EffectiveFrom)
            {
                ErrorMessage = "Effective Until must be on or after Effective From.";
                return Page();
            }

            // Validate category exists and matches the selected budget type
            CategoryDto? selectedCategory = Categories.FirstOrDefault(c => c.Id == CategoryId);
            if (selectedCategory is null)
            {
                ErrorMessage = "Please select a valid category.";
                return Page();
            }

            if (!string.Equals(selectedCategory.Type, BudgetType, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = $"The selected category '{selectedCategory.Name}' is not of type {BudgetType}.";
                return Page();
            }

            await _mediator.Send(new CreateBudgetCommand(
                CategoryId,
                LimitAmount,
                EffectiveFrom,
                EffectiveUntil,
                granularity));

            TempData["SuccessMessage"] = "Budget created successfully.";
            return RedirectToPage("/Budgets/Index");
        }
        catch (DomainException ex)
        {
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
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
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
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
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

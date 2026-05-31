using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.Commands;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class AddModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public AddTransactionInputModel Input { get; set; } = new();

    public List<CategoryDto> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public AddModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    internal async Task<Guid?> ResolveCategoryIdAsync(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        var match = Categories.FirstOrDefault(c =>
            c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            return match.Id;

        var type = Input.Amount >= 0 ? CategoryType.Income : CategoryType.Expense;
        return await _mediator.Send(new CreateCategoryCommand(categoryName, type));
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
        Categories = await _mediator.Send(new GetCategoriesQuery());

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var categoryId = await ResolveCategoryIdAsync(Input.CategoryName);
            var transactionId = await _mediator.Send(new CreateTransactionCommand(
                Input.Amount,
                Input.Currency ?? "EUR",
                Input.Date,
                Input.Description,
                categoryId));

            TempData["SuccessMessage"] = "Transaction added successfully.";
            return RedirectToPage("/Transactions/Index");
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Add.OnPostAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Add.OnPostAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred. Please try again later.";
            return Page();
        }
    }
}

public class AddTransactionInputModel
{
    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public decimal Amount { get; set; }

    public string? Currency { get; set; } = "EUR";

    [StringLength(500)]
    public string? CategoryName { get; set; }
}

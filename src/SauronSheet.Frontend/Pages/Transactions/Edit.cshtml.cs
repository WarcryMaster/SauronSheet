using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Application.Features.Subcategories.Queries;
using SauronSheet.Application.Features.Subcategories.DTOs;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
[ValidateAntiForgeryToken]
public class EditModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public decimal Amount { get; set; }

    [BindProperty]
    public string Currency { get; set; } = "EUR";

    [BindProperty]
    public DateTime Date { get; set; }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public Guid? CategoryId { get; set; }

    [BindProperty]
    public Guid? SubcategoryId { get; set; }

    public TransactionDto? Transaction { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
    public List<SubcategoryDto> AllSubcategories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public EditModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Transaction = await _mediator.Send(new GetTransactionByIdQuery(Id));

            // Populate form with current values
            Amount = Transaction.Amount;
            Currency = Transaction.Currency;
            Date = Transaction.Date;
            Description = Transaction.Description;
            CategoryId = Transaction.CategoryId;
            SubcategoryId = !string.IsNullOrEmpty(Transaction.SubcategoryId)
                ? Guid.Parse(Transaction.SubcategoryId)
                : null;

            // Load categories and subcategories for dropdowns
            Categories = await _mediator.Send(new GetCategoriesQuery());
            AllSubcategories = await _mediator.Send(new GetAllSubcategoriesQuery());

            return Page();
        }
        catch (EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Transaction not found.";
            return RedirectToPage("/Transactions/Index");
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Transactions/Edit.OnGetAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            TempData["ErrorMessage"] = "A network error occurred. Please check your connection and try again.";
            return RedirectToPage("/Transactions/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await ReloadFormDataAsync();
            return Page();
        }

        try
        {
            CategoryId? categoryId = CategoryId.HasValue ? new CategoryId(CategoryId.Value) : null;
            SubcategoryId? subcategoryId = SubcategoryId.HasValue ? new SubcategoryId(SubcategoryId.Value) : null;

            await _mediator.Send(new UpdateTransactionCommand(
                new TransactionId(Id),
                Amount,
                Currency,
                Date,
                Description,
                categoryId,
                subcategoryId));

            TempData["SuccessMessage"] = "Transaction updated successfully.";
            return RedirectToPage("/Transactions/Index");
        }
        catch (EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "Transaction not found.";
            return RedirectToPage("/Transactions/Index");
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            await ReloadFormDataAsync();
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
                scope.SetTag("page", "Transactions/Edit.OnPostAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            await ReloadFormDataAsync();
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Transactions/Edit.OnPostAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred. Please try again later.";
            await ReloadFormDataAsync();
            return Page();
        }
    }

    private async Task ReloadFormDataAsync()
    {
        try
        {
            Transaction = await _mediator.Send(new GetTransactionByIdQuery(Id));
            Categories = await _mediator.Send(new GetCategoriesQuery());
            AllSubcategories = await _mediator.Send(new GetAllSubcategoriesQuery());
        }
        catch
        {
            Transaction = null;
        }
    }
}

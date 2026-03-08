using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.Exceptions;
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

    public async Task OnGetAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var transactionId = await _mediator.Send(new CreateTransactionCommand(
                Input.Amount,
                Input.Currency ?? "EUR",
                Input.Date,
                Input.Description,
                Input.CategoryId));

            TempData["SuccessMessage"] = "Transaction added successfully.";
            return RedirectToPage("/Transactions/Index");
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Add.OnPostAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Add.OnPostAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = $"An error occurred: {ex.GetType().Name}: {ex.Message}";
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

    public Guid? CategoryId { get; set; }
}

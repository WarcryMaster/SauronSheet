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

    [BindProperty]
    public DateTime Month { get; set; } = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

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
            return RedirectToPage("/Auth/Login");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Categories = await _mediator.Send(new GetCategoriesQuery());

            var periodStart = new DateTime(Month.Year, Month.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            await _mediator.Send(new CreateBudgetCommand(
                CategoryId,
                LimitAmount,
                periodStart,
                periodEnd));

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
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception)
        {
            ErrorMessage = "An error occurred while creating the budget.";
            Categories = await _mediator.Send(new GetCategoriesQuery());
            return Page();
        }
    }
}

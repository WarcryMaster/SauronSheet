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
            return RedirectToPage("/Auth/Login");
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid budgetId)
    {
        try
        {
            await _mediator.Send(new DeleteBudgetCommand(budgetId));
            SuccessMessage = "Budget deleted successfully.";
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "An error occurred while deleting the budget.";
        }

        Budgets = await _mediator.Send(new GetBudgetsQuery(Year, Month));
        return Page();
    }
}

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
public class EditModel : PageModel
{
    private readonly IMediator _mediator;

    public BudgetStatusDto? Budget { get; set; }

    [BindProperty]
    public Guid BudgetId { get; set; }

    [BindProperty]
    public decimal NewLimitAmount { get; set; }

    public string? ErrorMessage { get; set; }

    public EditModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Budget = await _mediator.Send(new GetBudgetByIdQuery(id));
            BudgetId = id;
            NewLimitAmount = Budget.LimitAmount;
            return Page();
        }
        catch (EntityNotFoundException)
        {
            return RedirectToPage("/Budgets/Index");
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
            await _mediator.Send(new UpdateBudgetCommand(BudgetId, NewLimitAmount));
            TempData["SuccessMessage"] = "Budget updated successfully.";
            return RedirectToPage("/Budgets/Index");
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            Budget = await _mediator.Send(new GetBudgetByIdQuery(BudgetId));
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception)
        {
            ErrorMessage = "An error occurred while updating the budget.";
            try { Budget = await _mediator.Send(new GetBudgetByIdQuery(BudgetId)); } catch { }
            return Page();
        }
    }
}

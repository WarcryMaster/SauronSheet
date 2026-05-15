using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;

namespace SauronSheet.Frontend.Pages.Budgets;

[Authorize]
public class ComparisonModel : PageModel
{
    private readonly IMediator _mediator;

    public List<BudgetVsActualDto> Comparison { get; set; } = new();
    public decimal TotalBudgeted { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalDifference { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    public ComparisonModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var year = Year ?? DateTime.UtcNow.Year;
            var month = Month ?? DateTime.UtcNow.Month;

            Comparison = await _mediator.Send(new GetBudgetVsActualQuery(year, month));

            TotalBudgeted = Comparison.Where(c => c.BudgetLimit.HasValue).Sum(c => c.BudgetLimit!.Value);
            TotalActual = Comparison.Sum(c => c.ActualSpend);
            TotalDifference = TotalBudgeted - TotalActual;

            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Budgets;

[Authorize]
public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public BudgetStatusDto? Budget { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new();

    public DetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Budget = await _mediator.Send(new GetBudgetByIdQuery(id));

            // Load transactions for this category and period
            var searchResult = await _mediator.Send(new SearchTransactionsQuery(
                Keyword: null,
                FromDate: Budget.PeriodStart,
                ToDate: Budget.PeriodEnd,
                CategoryId: Budget.CategoryId,
                MinAmount: null,
                MaxAmount: null,
                Page: 1,
                PageSize: 100));

            Transactions = searchResult.Items;

            return Page();
        }
        catch (EntityNotFoundException)
        {
            return RedirectToPage("/budgets/index");
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
    }
}

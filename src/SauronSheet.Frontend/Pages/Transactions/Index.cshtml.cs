using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Application.Features.Transactions.DTOs;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public PaginatedResultDto<TransactionDto> Transactions { get; set; } = 
        new PaginatedResultDto<TransactionDto>(new List<TransactionDto>(), 0, 1, 50, 0);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        Transactions = await _mediator.Send(
            new GetTransactionsQuery(PageNumber, PageSize, CategoryId, StartDate, EndDate));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteTransactionCommand(id));
            TempData["SuccessMessage"] = "Transaction deleted successfully.";
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Index.OnPostDeleteAsync");
                scope.SetTag("transactionId", id.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            TempData["ErrorMessage"] = $"Error deleting transaction: {ex.Message}";
        }

        return RedirectToPage();
    }
}

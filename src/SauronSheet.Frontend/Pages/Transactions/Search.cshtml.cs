using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Application.Features.Categories.Queries;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Queries;

namespace SauronSheet.Frontend.Pages.Transactions;

/// <summary>
/// Search page model with multi-filter transaction search.
/// Phase 4 (US5): Keyword, date, category, amount filters with pagination.
/// </summary>
[Authorize]
public class SearchModel : PageModel
{
    private readonly IMediator _mediator;

    public PaginatedResultDto<TransactionDto>? Results { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public SearchModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Categories = await _mediator.Send(new GetCategoriesQuery());

            Results = await _mediator.Send(new SearchTransactionsQuery(
                Keyword: Keyword,
                FromDate: FromDate,
                ToDate: ToDate,
                CategoryId: CategoryId,
                MinAmount: MinAmount,
                MaxAmount: MaxAmount,
                Page: PageNumber,
                PageSize: 50));

            return Page();
        }
        catch (UnauthorizedAccessException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Search.OnGetAsync");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Search.OnGetAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            // Redirigir a página de error genérica
            return RedirectToPage("/Error");
        }
    }
}

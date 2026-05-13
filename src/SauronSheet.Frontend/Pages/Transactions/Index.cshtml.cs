using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using System.Text.Json;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.ValueObjects;

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
            TempData["ErrorMessage"] = "An unexpected error occurred while deleting the transaction. Please try again later.";
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Feature 004: Bulk delete handler
    /// Expects JSON array of transaction IDs from client (Feature 004 bulk-delete.js)
    /// Dispatches BulkDeleteTransactionsCommand with user context for multi-tenant isolation
    /// </summary>
    public async Task<IActionResult> OnPostBulkDeleteAsync()
    {
        try
        {
            // Parse transaction IDs from form JSON input
            var transactionIdsJson = Request.Form["transactionIds"].ToString();
            if (string.IsNullOrEmpty(transactionIdsJson))
            {
                TempData["ErrorMessage"] = "No transactions selected for deletion.";
                return RedirectToPage();
            }

            var idsArray = JsonSerializer.Deserialize<string[]>(transactionIdsJson) ?? Array.Empty<string>();
            if (idsArray.Length == 0)
            {
                TempData["ErrorMessage"] = "No transactions selected for deletion.";
                return RedirectToPage();
            }

            // Convert to TransactionId value objects
            var transactionIds = idsArray
                .Select(id => new TransactionId(Guid.Parse(id)))
                .ToList();

            // Get user ID from claims (multi-tenant isolation)
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Sentry.SentrySdk.Logger?.LogError("OnPostBulkDeleteAsync: No user ID found in claims");
                TempData["ErrorMessage"] = "Authentication error. Please log in again.";
                return RedirectToPage();
            }

            // Dispatch command with retry strategy (handled by handler)
            var result = await _mediator.Send(
                new BulkDeleteTransactionsCommand(new UserId(userId), transactionIds));

            // Handle deletion result
            if (result.Count > 0)
            {
                TempData["SuccessMessage"] = $"Successfully deleted {result.Count} transaction(s).";
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                // Distinguish network errors (show retry hint) from business errors
                if (result.ErrorMessage.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                    result.ErrorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["WarningMessage"] = $"Deletion partially failed: {result.ErrorMessage} You can retry from the dashboard.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Deletion error: {result.ErrorMessage}";
                }
            }
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("page", "Transactions/Index.OnPostBulkDeleteAsync");
                scope.SetTag("action", "bulk-delete");
                scope.Level = Sentry.SentryLevel.Error;
            });
            TempData["ErrorMessage"] = "An unexpected error occurred during bulk deletion. Please try again later.";
        }

        return RedirectToPage();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public IFormFile? ExcelFile { get; set; }

    public ImportResultDto? ImportResult { get; set; }
    public string? ErrorMessage { get; set; }

    public UploadModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ExcelFile == null || ExcelFile.Length == 0)
        {
            ErrorMessage = "Please select an Excel file.";
            return Page();
        }

        if (ExcelFile.Length > 10 * 1024 * 1024) // 10MB
        {
            ErrorMessage = "File size exceeds 10MB limit.";
            return Page();
        }

        var filename = ExcelFile.FileName;
        if (!filename.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
            !filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Only Excel files (.xls, .xlsx) are accepted.";
            return Page();
        }

        try
        {
            using var stream = ExcelFile.OpenReadStream();
            ImportResult = await _mediator.Send(
                new ImportTransactionsCommand(stream, filename));
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Transactions/Upload.OnPostAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "Network error. Please check your connection and try again.";
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Transactions/Upload.OnPostAsync");
                scope.SetTag("exception_type", "DomainException");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Transactions/Upload.OnPostAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred. Please try again later.";
        }

        return Page();
    }
}

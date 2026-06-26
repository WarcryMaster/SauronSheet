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
    public IFormFile[] ExcelFiles { get; set; } = Array.Empty<IFormFile>();

    public List<ImportResultDto> ImportResults { get; set; } = new();
    public List<string> FileErrors { get; set; } = new();

    public UploadModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ExcelFiles == null || ExcelFiles.Length == 0)
        {
            FileErrors.Add("Please select at least one Excel file.");
            return Page();
        }

        foreach (var file in ExcelFiles)
        {
            if (file.Length == 0)
            {
                FileErrors.Add($"{file.FileName}: File is empty.");
                continue;
            }

            if (file.Length > 10 * 1024 * 1024) // 10MB
            {
                FileErrors.Add($"{file.FileName}: File size exceeds 10MB limit.");
                continue;
            }

            var filename = file.FileName;
            if (!filename.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                !filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                FileErrors.Add($"{filename}: Only Excel files (.xls, .xlsx) are accepted.");
                continue;
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _mediator.Send(
                    new ImportTransactionsCommand(stream, filename));
                ImportResults.Add(result);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage("/auth/login");
            }
            catch (HttpRequestException ex)
            {
                Sentry.SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetTag("page", "Transactions/Upload.OnPostAsync");
                    scope.SetTag("exception_type", ex.GetType().Name);
                    scope.SetTag("filename", filename);
                    scope.Level = Sentry.SentryLevel.Error;
                });
                FileErrors.Add($"{filename}: Network error. Please check your connection and try again.");
            }
            catch (DomainException ex)
            {
                FileErrors.Add($"{filename}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Sentry.SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetTag("page", "Transactions/Upload.OnPostAsync");
                    scope.SetTag("exception_type", ex.GetType().Name);
                    scope.SetTag("filename", filename);
                    scope.Level = Sentry.SentryLevel.Error;
                });
                FileErrors.Add($"{filename}: An unexpected error occurred. Please try again later.");
            }
        }

        return Page();
    }
}

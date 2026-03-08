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
    public IFormFile? PdfFile { get; set; }

    public ImportResultDto? ImportResult { get; set; }
    public string? ErrorMessage { get; set; }

    public UploadModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (PdfFile == null || PdfFile.Length == 0)
        {
            ErrorMessage = "Please select a PDF file.";
            return Page();
        }

        if (PdfFile.Length > 10 * 1024 * 1024) // 10MB
        {
            ErrorMessage = "File size exceeds 10MB limit.";
            return Page();
        }

        if (!PdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Only PDF files are accepted.";
            return Page();
        }

        // CRITICAL FIX NC-2: Add comprehensive error handling
        try
        {
            using var stream = PdfFile.OpenReadStream();
            ImportResult = await _mediator.Send(
                new ImportTransactionsFromPdfCommand(stream, PdfFile.FileName));

            // Metrics: PDF import results
            Sentry.SentrySdk.Experimental.Metrics.EmitCounter("app.pdf.import.count", 1.0,
                new KeyValuePair<string, object>[] { new("result", "success") });
            Sentry.SentrySdk.Experimental.Metrics.EmitDistribution("app.pdf.import.rows_imported",
                ImportResult.ImportedCount,
                Sentry.MeasurementUnit.None,
                new KeyValuePair<string, object>[] { new("type", "imported") });
            Sentry.SentrySdk.Experimental.Metrics.EmitDistribution("app.pdf.import.rows_skipped",
                ImportResult.SkippedCount,
                Sentry.MeasurementUnit.None,
                new KeyValuePair<string, object>[] { new("type", "skipped") });
        }
        catch (HttpRequestException)
        {
            // Network error (Supabase offline, timeout, etc.)
            ErrorMessage = "Network error. Please check your connection and try again.";
            // TODO Phase 6: Log exception for diagnostics
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("PDF"))
        {
            // PDF parsing error (from GenericBankPdfParser - NC-3)
            ErrorMessage = $"Could not parse PDF: {ex.Message}";
        }
        catch (DomainException ex)
        {
            // Domain validation error (future date, etc.)
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            // Unexpected error
            ErrorMessage = "An unexpected error occurred. Please try again later.";
            // TODO Phase 6: Log exception for diagnostics
        }

        return Page();
    }
}

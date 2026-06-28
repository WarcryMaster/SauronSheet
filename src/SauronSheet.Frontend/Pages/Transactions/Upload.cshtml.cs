using System.Net;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Services;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Transactions;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IImportProgressTracker? _progressTracker;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    [BindProperty]
    public IFormFile[] ExcelFiles { get; set; } = Array.Empty<IFormFile>();

    public List<ImportResultDto> ImportResults { get; set; } = new();
    public List<string> FileErrors { get; set; } = new();

    public UploadModel(
        IMediator mediator,
        IServiceScopeFactory serviceScopeFactory,
        IImportProgressTracker? progressTracker = null)
    {
        _mediator = mediator;
        _progressTracker = progressTracker;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnGetProgress(string id)
    {
        if (_progressTracker == null)
        {
            return NotFound();
        }

        string? currentUserId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Forbid();
        }

        ImportProgress? progress = _progressTracker.GetProgress(id);
        if (progress == null)
        {
            return NotFound();
        }

        if (progress.UserId != currentUserId)
        {
            return Forbid();
        }

        if (progress.IsComplete || progress.IsFailed)
        {
            Response.Headers["HX-Trigger"] = "{\"stopPolling\": true}";
        }

        string html = BuildProgressHtml(progress);
        return Content(html, "text/html");
    }

    public async Task<IActionResult> OnPostUploadAsync(CancellationToken cancellationToken = default)
    {
        if (ExcelFiles == null || ExcelFiles.Length == 0)
        {
            return BadRequest(new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Please select at least one Excel file."
            });
        }

        List<string> validationErrors = new();
        List<IFormFile> validFiles = new();

        foreach (IFormFile file in ExcelFiles)
        {
            if (file.Length == 0)
            {
                validationErrors.Add($"{file.FileName}: File is empty.");
                continue;
            }

            if (file.Length > 10 * 1024 * 1024) // 10MB
            {
                validationErrors.Add($"{file.FileName}: File size exceeds 10MB limit.");
                continue;
            }

            string filename = file.FileName;
            if (!filename.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                !filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add($"{filename}: Only Excel files (.xls, .xlsx) are accepted.");
                continue;
            }

            validFiles.Add(file);
        }

        if (validFiles.Count == 0)
        {
            return BadRequest(new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = string.Join(" ", validationErrors)
            });
        }

        string uploadId = Guid.NewGuid().ToString("N");
        string userId = User.FindFirst("sub")?.Value ?? string.Empty;
        string firstFileName = validFiles[0].FileName;
        int totalFiles = validFiles.Count;

        // Initialize tracker so the frontend can start polling immediately.
        // totalRows=0 here; the handler will re-initialize with the real count after parsing.
        if (_progressTracker != null)
        {
            await _progressTracker.InitializeAsync(
                uploadId,
                firstFileName,
                0,
                userId,
                firstFileName,
                currentFileIndex: 1,
                totalFiles,
                cancellationToken);
        }

        // Copy file contents to memory — IFormFile streams are request-scoped.
        List<(string FileName, byte[] Content)> fileData = new(validFiles.Count);
        foreach (IFormFile file in validFiles)
        {
            using MemoryStream ms = new();
            await file.CopyToAsync(ms, cancellationToken);
            fileData.Add((file.FileName, ms.ToArray()));
        }

        // Capture user principal and access token BEFORE Task.Run so we can
        // reconstruct the HttpContext in the background scope. HttpUserContext
        // depends on IHttpContextAccessor, and Supabase.Client reads the
        // sb-access-token cookie for RLS compliance — both are null outside
        // the HTTP request pipeline.
        ClaimsPrincipal capturedUser = User;
        string? accessToken = Request.Cookies["sb-access-token"];

        // Fire-and-forget: process files in a background scope so the frontend
        // can poll progress while the handler reports it via IMemoryCache.
        // Per spec REQ-PROG-005: upload does not reload the page.
        _ = Task.Run(async () =>
        {
            IHttpContextAccessor? httpContextAccessor = null;
            try
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();

                // Reconstruct a fake HttpContext with the captured user and access
                // token cookie. This allows:
                //   1. HttpUserContext (IUserContext) to resolve UserId correctly
                //   2. Supabase.Client factory to read sb-access-token for RLS
                httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                DefaultHttpContext fakeContext = new() { User = capturedUser };
                if (!string.IsNullOrEmpty(accessToken))
                {
                    fakeContext.Request.Headers["Cookie"] = $"sb-access-token={accessToken}";
                }
                httpContextAccessor.HttpContext = fakeContext;

                IMediator scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                IImportProgressTracker? scopedTracker = scope.ServiceProvider.GetService<IImportProgressTracker>();

                int fileIndex = 0;
                foreach ((string fileName, byte[] content) in fileData)
                {
                    fileIndex++;
                    if (scopedTracker != null && totalFiles > 1)
                    {
                        await scopedTracker.UpdateCurrentFileAsync(uploadId, fileName, fileIndex, CancellationToken.None);
                    }

                    try
                    {
                        using MemoryStream stream = new(content);
                        await scopedMediator.Send(
                            new ImportTransactionsCommand(stream, fileName, uploadId),
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        if (scopedTracker != null)
                        {
                            string safeMessage = ex is DomainException
                                ? ex.Message
                                : "An unexpected error occurred.";
                            await scopedTracker.FailAsync(uploadId, $"{fileName}: {safeMessage}");
                        }

                        Sentry.SentrySdk.CaptureException(ex, sentryScope =>
                        {
                            sentryScope.SetTag("page", "Transactions/Upload.OnPostUploadAsync.background");
                            sentryScope.SetTag("exception_type", ex.GetType().Name);
                            sentryScope.SetTag("filename", fileName);
                            sentryScope.SetTag("uploadId", uploadId);
                            sentryScope.Level = Sentry.SentryLevel.Error;
                        });

                        return; // Stop on first failure
                    }
                }

                if (scopedTracker != null)
                {
                    await scopedTracker.CompleteAsync(uploadId);
                }
            }
            catch (Exception ex)
            {
                Sentry.SentrySdk.CaptureException(ex, sentryScope =>
                {
                    sentryScope.SetTag("page", "Transactions/Upload.OnPostUploadAsync.background");
                    sentryScope.SetTag("exception_type", ex.GetType().Name);
                    sentryScope.SetTag("uploadId", uploadId);
                    sentryScope.Level = Sentry.SentryLevel.Fatal;
                });
            }
            finally
            {
                // Clean up the fake HttpContext to prevent leaking across async flows.
                if (httpContextAccessor != null)
                {
                    if (httpContextAccessor.HttpContext is IDisposable d)
                        d.Dispose();
                    httpContextAccessor.HttpContext = null;
                }
            }
        });

        return new JsonResult(new Dictionary<string, object>
        {
            ["uploadId"] = uploadId,
            ["success"] = true
        });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ExcelFiles == null || ExcelFiles.Length == 0)
        {
            FileErrors.Add("Please select at least one Excel file.");
            return Page();
        }

        foreach (IFormFile file in ExcelFiles)
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

            string filename = file.FileName;
            if (!filename.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                !filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                FileErrors.Add($"{filename}: Only Excel files (.xls, .xlsx) are accepted.");
                continue;
            }

            try
            {
                using Stream stream = file.OpenReadStream();
                ImportResultDto result = await _mediator.Send(
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

    private static string BuildProgressHtml(ImportProgress progress)
    {
        if (progress.IsFailed)
        {
            string errorMessage = WebUtility.HtmlEncode(progress.ErrorMessage ?? "An unexpected error occurred.");
            return $"""
                <div class="alert alert-danger" role="alert">
                  <p class="fw-semibold">Import failed</p>
                  <p class="small">{errorMessage}</p>
                  <button type="button" class="btn btn-outline-danger btn-sm" @click="uploading = false; progressVisible = false">Try again</button>
                </div>
                """;
        }

        int percentage = progress.TotalRows > 0
            ? progress.ProcessedRows * 100 / progress.TotalRows
            : 0;
        string currentFileName = WebUtility.HtmlEncode(progress.CurrentFileName);
        string filename = WebUtility.HtmlEncode(progress.Filename);

        if (progress.IsComplete)
        {
            return $"""
                <div class="alert alert-success" role="status">
                  <p class="fw-semibold">Import completed</p>
                  <p class="small">{filename}: {progress.ImportedCount} imported, {progress.SkippedCount} skipped.</p>
                </div>
                """;
        }

        return $"""
            <div id="progress-container" role="progressbar"
                 aria-valuenow="{percentage}" aria-valuemin="0" aria-valuemax="100"
                 aria-label="Import progress: {percentage}%">
              <p class="small text-muted mb-1">Processing file {progress.CurrentFileIndex} of {progress.TotalFiles}: {currentFileName}</p>
              <div class="progress" style="height: 20px;">
                <div class="progress-bar" style="width: {percentage}%">{percentage}%</div>
              </div>
              <p class="small mt-1">{progress.ProcessedRows}/{progress.TotalRows} rows | Imported: {progress.ImportedCount} | Skipped: {progress.SkippedCount}</p>
            </div>
            """;
    }
}

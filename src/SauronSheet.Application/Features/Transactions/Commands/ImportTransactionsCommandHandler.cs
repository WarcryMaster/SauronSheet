namespace SauronSheet.Application.Features.Transactions.Commands;

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using Sentry;
using Sentry.Extensibility;
using SauronSheet.Application.Services;

/// <summary>
/// Handles <see cref="ImportTransactionsCommand"/>.
///
/// Orchestrates the neutral Excel import flow:
///   1. Validates the file extension (.xls / .xlsx).
///   2. Calls <see cref="IStatementParser.ParseAsync"/> to obtain validated rows.
///   3. For each row: checks cross-store duplicates, resolves categories, persists the transaction.
///   4. Persists import-batch metadata via <see cref="IImportBatchRepository"/>.
///
/// Row-level errors from the parser are converted to <c>ImportRowErrorDto</c> entries.
/// In-file hash duplicates (<c>StatementParseResult.SkippedCount</c>) are silent per spec ESP-3b.
/// Non-domain exceptions from the parser are captured to Sentry and re-thrown as a generic
/// <see cref="DomainException"/> so internal infrastructure details are never leaked to users.
/// </summary>
public class ImportTransactionsCommandHandler
    : IRequestHandler<ImportTransactionsCommand, ImportResultDto>
{
    private readonly IStatementParser _statementParser;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IImportBatchRepository _importBatchRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IUserContext _userContext;
    private readonly IBankCategoryResolutionService _resolutionService;
    private readonly IImportProgressTracker? _progressTracker;

    public ImportTransactionsCommandHandler(
        IStatementParser statementParser,
        ITransactionRepository transactionRepo,
        IImportBatchRepository importBatchRepo,
        IUserProfileRepository userProfileRepo,
        IUserContext userContext,
        IBankCategoryResolutionService resolutionService,
        IImportProgressTracker? progressTracker = null)
    {
        _statementParser = statementParser;
        _transactionRepo = transactionRepo;
        _importBatchRepo = importBatchRepo;
        _userProfileRepo = userProfileRepo;
        _userContext = userContext;
        _resolutionService = resolutionService;
        _progressTracker = progressTracker;
    }

    public async Task<ImportResultDto> Handle(
        ImportTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        // Force InvariantCulture so decimal serialization uses dot (not comma),
        // preventing Postgrest numeric input errors with comma-separated values.
        CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        try
        {
            if (request.FileStream == null)
                throw new ArgumentException("File stream is required.", nameof(request.FileStream));

            if (!request.Filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !request.Filename.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                throw new DomainException("Only Excel files (.xls, .xlsx) are accepted.");

            // ── Sentry observability: import started ──────────────────────────────
            string fileExt = Path.GetExtension(request.Filename).ToLowerInvariant();

            SentrySdk.Logger?.LogDebug(
                "ImportTransactionsCommandHandler: starting import for {0}", request.Filename);

            SentrySdk.AddBreadcrumb(
                $"Excel import started: {request.Filename}",
                "import",
                data: new Dictionary<string, string>
                {
                    { "filename", request.Filename },
                    { "ext", fileExt }
                });

            SentrySdk.Experimental.Metrics.EmitCounter(
                "app.import.started",
                1.0,
                new KeyValuePair<string, object>[] { new("ext", fileExt) });
            // ─────────────────────────────────────────────────────────────────────

            UserId userId = new UserId(_userContext.UserId);

            // Ensure user profile exists before FK-constrained inserts.
            await _userProfileRepo.EnsureExistsAsync(userId, _userContext.UserEmail);

            // Parse statement.
            // DomainExceptions (invalid sheet / header) are user-safe — propagate as-is.
            // All other exceptions are infrastructure failures — capture to Sentry, throw generic.
            StatementParseResult parseResult;
            try
            {
                parseResult = await _statementParser.ParseAsync(request.FileStream, request.Filename);
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetTag("handler", "ImportTransactionsCommandHandler.ParseAsync");
                    scope.SetTag("filename", request.Filename);
                    scope.Level = SentryLevel.Error;
                });
                SentrySdk.Experimental.Metrics.EmitCounter(
                    "app.import.failed",
                    1.0,
                    new KeyValuePair<string, object>[]
                    {
                        new("ext", fileExt),
                        new("reason", "parse_error")
                    });
                throw new DomainException(
                    "Could not parse the uploaded file. Please check the format and try again.");
            }

            int importedCount = 0;
            // In-file hash duplicates are silent (spec ESP-3b) — counted in skipped, no error entry.
            int skippedCount = parseResult.SkippedCount;
            List<ImportRowErrorDto> errors = new List<ImportRowErrorDto>();

            // Convert parser-level row errors to ImportRowErrorDto.
            foreach (var rowError in parseResult.RowErrors)
            {
                errors.Add(new ImportRowErrorDto(rowError.RowNumber, rowError.RawContent, rowError.Reason));
                skippedCount++;
            }

            int totalRows = parseResult.Rows.Count + parseResult.RowErrors.Count + parseResult.SkippedCount;
            int processedCount = parseResult.RowErrors.Count + parseResult.SkippedCount;

            if (_progressTracker != null && request.UploadId != null)
            {
                await _progressTracker.InitializeAsync(
                    request.UploadId,
                    request.Filename,
                    totalRows,
                    _userContext.UserId,
                    request.Filename,
                    currentFileIndex: 1,
                    totalFiles: 1,
                    cancellationToken);
            }

            try
            {
                // Process validated rows.
                foreach (var row in parseResult.Rows)
                {
                    processedCount++;

                    try
                    {
                        // Parse date. Parser guarantees dd/MM/yyyy for rows in result.Rows.
                        if (!DateTime.TryParseExact(
                                row.Date,
                                new[] { "dd/MM/yyyy", "d/M/yyyy" },
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var date))
                        {
                            errors.Add(new ImportRowErrorDto(
                                row.RowNumber,
                                row.Date ?? string.Empty,
                                "Invalid date format"));
                            skippedCount++;
                            await ReportProgressIfActiveAsync(
                                request.UploadId, totalRows, processedCount, importedCount, skippedCount, cancellationToken);
                            continue;
                        }

                        // TZ-FIX: Normalize to UTC so TIMESTAMPTZ stores it correctly.
                        // ParseExact with no timezone info produces Unspecified Kind,
                        // which would be interpreted as local time by PostgreSQL.
                        date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                        // Parse amount. Parser guarantees parseable values for rows in result.Rows.
                        if (!TryParseAmount(row.Amount, out var amount))
                        {
                            errors.Add(new ImportRowErrorDto(
                                row.RowNumber,
                                row.Amount ?? string.Empty,
                                "Invalid amount format"));
                            skippedCount++;
                            await ReportProgressIfActiveAsync(
                                request.UploadId, totalRows, processedCount, importedCount, skippedCount, cancellationToken);
                            continue;
                        }

                        // Parse balance (nullable — not all statements provide it).
                        decimal? balance = null;
                        if (!string.IsNullOrWhiteSpace(row.Balance)
                            && TryParseAmount(row.Balance, out var parsedBalance))
                        {
                            balance = parsedBalance;
                        }

                        // Check cross-store duplicates (date + amount + description + balance).
                        bool isDuplicate = await _transactionRepo.ExistsDuplicateAsync(
                            userId, date, amount, row.Description ?? string.Empty, balance);

                        if (isDuplicate)
                        {
                            errors.Add(new ImportRowErrorDto(
                                row.RowNumber,
                                $"{date:yyyy-MM-dd} | {row.Description} | {amount}",
                                "Duplicate"));
                            skippedCount++;
                            await ReportProgressIfActiveAsync(
                                request.UploadId, totalRows, processedCount, importedCount, skippedCount, cancellationToken);
                            continue;
                        }

                        // Resolve bank category via get-or-add (spec PCE-3 / PCE-4).
                        ResolutionResult resolution = await _resolutionService.ResolveOrCreateAsync(
                            userId, row.Category, row.SubCategory, amount, cancellationToken);

                        // Persist transaction.
                        // CR-1c: trim bank category/subcategory to remove any surrounding whitespace.
                        Transaction transaction = new Transaction(
                            new TransactionId(Guid.NewGuid()),
                            userId,
                            new Money(amount, row.Currency ?? "EUR"),
                            date,
                            row.Description ?? string.Empty,
                            categoryId: resolution.CategoryId,
                            importedFrom: request.Filename,
                            bankCategory: row.Category?.Trim(),
                            bankSubcategory: row.SubCategory?.Trim(),
                            subcategoryId: resolution.SubcategoryId,
                            categorySource: resolution.CategorySource,
                            balance: balance);

                        await _transactionRepo.AddAsync(transaction);
                        importedCount++;
                    }
                    catch (DomainException ex)
                    {
                        errors.Add(new ImportRowErrorDto(
                            row.RowNumber,
                            $"{row.Date} | {row.Description} | {row.Amount}",
                            ex.Message));
                        skippedCount++;
                    }

                    await ReportProgressIfActiveAsync(
                        request.UploadId, totalRows, processedCount, importedCount, skippedCount, cancellationToken);
                }

                // Persist import-batch metadata.
                ImportBatch importBatch = new ImportBatch(
                    Guid.NewGuid(),
                    request.Filename,
                    importedCount,
                    skippedCount,
                    DateTime.UtcNow);

                await _importBatchRepo.AddAsync(importBatch, userId);

                // ── Sentry observability: import completed ────────────────────────────
                SentrySdk.Logger?.LogInfo(
                    "ImportTransactionsCommandHandler: import completed — {0}: {1} imported, {2} skipped, {3} errors",
                    request.Filename, importedCount, skippedCount, errors.Count);

                SentrySdk.AddBreadcrumb(
                    $"Excel import completed: {importedCount} imported, {skippedCount} skipped",
                    "import",
                    level: BreadcrumbLevel.Info,
                    data: new Dictionary<string, string>
                    {
                        { "filename", request.Filename },
                        { "imported", importedCount.ToString() },
                        { "skipped", skippedCount.ToString() },
                        { "errors", errors.Count.ToString() }
                    });

                SentrySdk.Experimental.Metrics.EmitCounter(
                    "app.import.completed",
                    1.0,
                    new KeyValuePair<string, object>[]
                    {
                        new("ext", fileExt),
                        new("result", errors.Count == 0 ? "success" : "partial_success")
                    });

                if (importedCount > 0)
                {
                    SentrySdk.Experimental.Metrics.EmitCounter(
                        "app.import.rows_imported",
                        importedCount,
                        new KeyValuePair<string, object>[] { new("ext", fileExt) });
                }
                // ─────────────────────────────────────────────────────────────────────

                if (_progressTracker != null && request.UploadId != null)
                {
                    await _progressTracker.CompleteAsync(request.UploadId);
                }

                return new ImportResultDto(
                    importedCount,
                    skippedCount,
                    importedCount + skippedCount,
                    request.Filename,
                    DateTime.UtcNow,
                    errors);
            }
            catch (Exception ex)
            {
                if (_progressTracker != null && request.UploadId != null)
                {
                    await _progressTracker.FailAsync(request.UploadId, ex.Message);
                }

                throw;
            }
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
            Thread.CurrentThread.CurrentUICulture = previousCulture;
        }
    }

    private async Task ReportProgressIfActiveAsync(
        string? uploadId,
        int totalRows,
        int processedRows,
        int importedCount,
        int skippedCount,
        CancellationToken cancellationToken)
    {
        if (_progressTracker == null || uploadId == null)
        {
            return;
        }

        bool shouldReport = totalRows <= 100 || processedRows % 10 == 0;
        if (!shouldReport)
        {
            return;
        }

        await _progressTracker.ReportProgressAsync(
            uploadId,
            processedRows,
            importedCount,
            skippedCount,
            ct: cancellationToken);
    }

    /// <summary>
    /// Tries to parse an amount string using invariant, Spanish, and comma-to-dot fallback cultures.
    /// </summary>
    private static bool TryParseAmount(string? amount, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(amount))
            return false;

        if (decimal.TryParse(amount, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return true;

        if (decimal.TryParse(amount, NumberStyles.Any, new CultureInfo("es-ES"), out result))
            return true;

        string normalized = amount.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}

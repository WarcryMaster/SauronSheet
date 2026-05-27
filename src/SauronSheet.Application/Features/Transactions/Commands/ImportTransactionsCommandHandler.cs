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

    public ImportTransactionsCommandHandler(
        IStatementParser statementParser,
        ITransactionRepository transactionRepo,
        IImportBatchRepository importBatchRepo,
        IUserProfileRepository userProfileRepo,
        IUserContext userContext,
        IBankCategoryResolutionService resolutionService)
    {
        _statementParser = statementParser;
        _transactionRepo = transactionRepo;
        _importBatchRepo = importBatchRepo;
        _userProfileRepo = userProfileRepo;
        _userContext = userContext;
        _resolutionService = resolutionService;
    }

    public async Task<ImportResultDto> Handle(
        ImportTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        // Force InvariantCulture so decimal serialization uses dot (not comma),
        // preventing Postgrest numeric input errors with comma-separated values.
        var previousCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        try
        {
            if (request.FileStream == null)
                throw new ArgumentException("File stream is required.", nameof(request.FileStream));

            if (!request.Filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !request.Filename.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                throw new DomainException("Only Excel files (.xls, .xlsx) are accepted.");

            var userId = new UserId(_userContext.UserId);

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
                throw new DomainException(
                    "Could not parse the uploaded file. Please check the format and try again.");
            }

            var importedCount = 0;
            // In-file hash duplicates are silent (spec ESP-3b) — counted in skipped, no error entry.
            var skippedCount = parseResult.SkippedCount;
            var errors = new List<ImportRowErrorDto>();

            // Convert parser-level row errors to ImportRowErrorDto.
            foreach (var rowError in parseResult.RowErrors)
            {
                errors.Add(new ImportRowErrorDto(rowError.RowNumber, rowError.RawContent, rowError.Reason));
                skippedCount++;
            }

            // Process validated rows.
            foreach (var row in parseResult.Rows)
            {
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
                        continue;
                    }

                    // Parse amount. Parser guarantees parseable values for rows in result.Rows.
                    if (!TryParseAmount(row.Amount, out var amount))
                    {
                        errors.Add(new ImportRowErrorDto(
                            row.RowNumber,
                            row.Amount ?? string.Empty,
                            "Invalid amount format"));
                        skippedCount++;
                        continue;
                    }

                    // Check cross-store duplicates.
                    var isDuplicate = await _transactionRepo.ExistsDuplicateAsync(
                        userId, date, amount, row.Description ?? string.Empty);

                    if (isDuplicate)
                    {
                        errors.Add(new ImportRowErrorDto(
                            row.RowNumber,
                            $"{date:yyyy-MM-dd} | {row.Description} | {amount}",
                            "Duplicate"));
                        skippedCount++;
                        continue;
                    }

                    // Resolve bank category via get-or-add (spec PCE-3 / PCE-4).
                    var resolution = await _resolutionService.ResolveOrCreateAsync(
                        userId, row.Category, row.SubCategory, cancellationToken);

                    // Persist transaction.
                    // CR-1c: trim bank category/subcategory to remove any surrounding whitespace.
                    var transaction = new Transaction(
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
                        categorySource: resolution.CategorySource);

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
            }

            // Persist import-batch metadata.
            var importBatch = new ImportBatch(
                Guid.NewGuid(),
                request.Filename,
                importedCount,
                skippedCount,
                DateTime.UtcNow);

            await _importBatchRepo.AddAsync(importBatch, userId);

            return new ImportResultDto(
                importedCount,
                skippedCount,
                importedCount + skippedCount,
                request.Filename,
                DateTime.UtcNow,
                errors);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
            Thread.CurrentThread.CurrentUICulture = previousCulture;
        }
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

        var normalized = amount.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}

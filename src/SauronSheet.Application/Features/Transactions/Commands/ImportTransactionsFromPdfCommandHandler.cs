namespace SauronSheet.Application.Features.Transactions.Commands;

using Categories.Commands;
using Domain.Common;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using Sentry;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class ImportTransactionsFromPdfCommandHandler
    : IRequestHandler<ImportTransactionsFromPdfCommand, ImportResultDto>
{
    private readonly IPdfParser _pdfParser;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IPdfImportRepository _pdfImportRepo;
    private readonly IUserProfileRepository _userProfileRepo;
    private readonly IUserContext _userContext;
    private readonly IMediator _mediator;

    public ImportTransactionsFromPdfCommandHandler(
        IPdfParser pdfParser,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IPdfImportRepository pdfImportRepo,
        IUserProfileRepository userProfileRepo,
        IUserContext userContext,
        IMediator mediator)
    {
        _pdfParser = pdfParser;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _pdfImportRepo = pdfImportRepo;
        _userProfileRepo = userProfileRepo;
        _userContext = userContext;
        _mediator = mediator;
    }

    public async Task<ImportResultDto> Handle(
        ImportTransactionsFromPdfCommand request,
        CancellationToken cancellationToken)
    {
        // Force InvariantCulture to ensure decimal serialization uses dot, not comma
        // This prevents Postgrest numeric input errors when sending decimals with comma separators
        var previousCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            if (request.PdfStream == null)
                throw new ArgumentException("PDF stream is required.", nameof(request.PdfStream));

            if (!request.Filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                throw new DomainException("Only PDF files are accepted.");

            var userId = new UserId(_userContext.UserId);

        // Ensure user profile exists in public.users before FK-constrained inserts.
        // Guards against the case where the Supabase trigger did not fire for this user.
        await _userProfileRepo.EnsureExistsAsync(userId, _userContext.UserEmail);

        // CLARIFICATION A-1: Seed system defaults via MediatR (NOT inline check)
        await _mediator.Send(new SeedSystemDefaultsCommand(), cancellationToken);

        // Parse PDF
        var rawRows = await _pdfParser.ParseAsync(request.PdfStream);

        var importedCount = 0;
        var skippedCount = 0;
        var errors = new List<ImportRowErrorDto>();

        foreach (var row in rawRows)
        {
            try
            {
                // Validate row
                SentrySdk.Logger?.LogDebug("[Import] Row {RowNumber}: Date='{Date}' Desc='{Description}' Amount='{Amount}' Category='{Category}' Currency='{Currency}'",
                    row.RowNumber,
                    row.Date ?? string.Empty,
                    row.Description ?? string.Empty,
                    row.Amount ?? string.Empty,
                    row.Category ?? string.Empty,
                    row.Currency ?? string.Empty);

                if (string.IsNullOrWhiteSpace(row.Date) ||
                    string.IsNullOrWhiteSpace(row.Description) ||
                    string.IsNullOrWhiteSpace(row.Amount))
                {
                    SentrySdk.Logger?.LogWarning("[Import] Row {RowNumber}: MISSING FIELDS — date='{Date}' desc='{Description}' amount='{Amount}'",
                        row.RowNumber,
                        row.Date ?? string.Empty,
                        row.Description ?? string.Empty,
                        row.Amount ?? string.Empty);
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        $"{row.Date} | {row.Description} | {row.Amount}",
                        "Missing required fields"));
                    skippedCount++;
                    continue;
                }

                // Parse date using dd/MM/yyyy (European format)
                if (!DateTime.TryParseExact(row.Date, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
                {
                    SentrySdk.Logger?.LogWarning("[Import] Row {RowNumber}: INVALID DATE — raw value: '{Date}'",
                        row.RowNumber,
                        row.Date ?? string.Empty);
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        row.Date ?? string.Empty,
                        "Invalid date format"));
                    skippedCount++;
                    continue;
                }

                // Parse amount with culture inference
                decimal amount;
                bool parsed = decimal.TryParse(row.Amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount);
                if (!parsed)
                {
                    // Try Spanish (comma decimal)
                    parsed = decimal.TryParse(row.Amount, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("es-ES"), out amount);
                }
                if (!parsed)
                {
                    // Try fallback: replace comma with dot and parse invariant
                    var normalized = row.Amount.Replace(',', '.');
                    parsed = decimal.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount);
                }
                if (!parsed)
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        row.Amount,
                        "Invalid amount format"));
                    skippedCount++;
                    continue;
                }

                var currency = row.Currency ?? "EUR";
                var description = row.Description;

                // CRITICAL FIX C-3: Check duplicate (ignores currency)
                var isDuplicate = await _transactionRepo.ExistsDuplicateAsync(
                    userId, date, amount, description);

                if (isDuplicate)
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        $"{date:yyyy-MM-dd} | {description} | {amount}",
                        "Duplicate"));
                    skippedCount++;
                    continue;
                }

                // Create transaction
                var transaction = new Transaction(
                    new TransactionId(Guid.NewGuid()),
                    userId,
                    new Money(amount, currency),
                    date,
                    description,
                    categoryId: null,
                    importedFrom: request.Filename);

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

        // CRITICAL FIX C-2: Save import metadata using IPdfImportRepository
        var importBatch = new ImportBatch(
            Guid.NewGuid(),
            request.Filename,
            importedCount,
            skippedCount,
            DateTime.UtcNow);

        await _pdfImportRepo.AddAsync(importBatch, new UserId(_userContext.UserId));

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
            // Restore previous culture
            System.Threading.Thread.CurrentThread.CurrentCulture = previousCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = previousCulture;
        }
    }
}

namespace SauronSheet.Application.Features.Transactions.Commands;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Models;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using DTOs;
using Interfaces;
using MediatR;
using Categories.Commands;

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
                if (string.IsNullOrWhiteSpace(row.Date) ||
                    string.IsNullOrWhiteSpace(row.Description) ||
                    string.IsNullOrWhiteSpace(row.Amount))
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        $"{row.Date} | {row.Description} | {row.Amount}",
                        "Missing required fields"));
                    skippedCount++;
                    continue;
                }

                // Parse date
                if (!DateTime.TryParse(row.Date, out var date))
                {
                    errors.Add(new ImportRowErrorDto(
                        row.RowNumber,
                        row.Date,
                        "Invalid date format"));
                    skippedCount++;
                    continue;
                }

                // Parse amount
                if (!decimal.TryParse(row.Amount, out var amount))
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
}

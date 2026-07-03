namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

using System.IO;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Moq;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Resources;
using SauronSheet.Application.Services;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using Xunit;

/// <summary>
/// Unit tests for ImportTransactionsCommandHandler.
/// Phase 2 (replace-pdf-import-with-excel): neutral Excel-based import flow.
///
/// Spec coverage:
///   IH-2a (happy path, 2 rows → 2 imported)
///   IH-2b (triangulation: 0 rows → batch still saved)
///   IH-2c (parser throws DomainException → surfaces as-is)
///   IH-2d (parser throws non-domain exception → generic DomainException + Sentry)
///   IH-2e (invalid file extension → DomainException)
///   IH-2f (cross-store duplicate → skipped + error in result)
///   IH-2g (parser RowErrors appear in ImportResultDto.Errors)
///   IH-2h (in-file skipped count reflected in result.SkippedCount)
///   IH-2i (category resolution mapped to Transaction)
///   T-PROG-003: progress tracker integration
/// </summary>
[Trait("Category", "Application")]
public class ImportTransactionsCommandHandlerTests
{
    private readonly Mock<IStatementParser> _mockParser;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IImportBatchRepository> _mockImportBatchRepo;
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<IBankCategoryResolutionService> _mockResolutionService;
    private readonly Mock<IImportProgressTracker> _mockProgressTracker;
    private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;

    public ImportTransactionsCommandHandlerTests()
    {
        _mockParser = new Mock<IStatementParser>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockImportBatchRepo = new Mock<IImportBatchRepository>();
        _mockUserProfileRepo = new Mock<IUserProfileRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockResolutionService = new Mock<IBankCategoryResolutionService>();
        _mockProgressTracker = new Mock<IImportProgressTracker>();
        _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();

        SetupLocalizerTranslations();

        _mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
        _mockUserContext.Setup(x => x.UserEmail).Returns("test@example.com");
        _mockUserProfileRepo
            .Setup(x => x.EnsureExistsAsync(It.IsAny<UserId>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockTransactionRepo
            .Setup(x => x.ExistsDuplicateAsync(
                It.IsAny<UserId>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockResolutionService
            .Setup(x => x.ResolveOrCreateAsync(
                It.IsAny<UserId>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolutionResult(null, null, CategorySource.RawOnly));
    }

    private ImportTransactionsCommandHandler CreateHandler() =>
        new(
            _mockParser.Object,
            _mockTransactionRepo.Object,
            _mockImportBatchRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object,
            _mockLocalizer.Object,
            null);

    private ImportTransactionsCommandHandler CreateHandlerWithTracker() =>
        new(
            _mockParser.Object,
            _mockTransactionRepo.Object,
            _mockImportBatchRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object,
            _mockLocalizer.Object,
            _mockProgressTracker.Object);

    private void SetupLocalizerTranslations()
    {
        Dictionary<string, string> englishTranslations = new Dictionary<string, string>
        {
            ["Import.Error.InvalidExtension"] = "Only Excel files (.xls, .xlsx) are accepted.",
            ["Import.Error.ParseFailed"] = "Could not parse the uploaded file. Please check the format and try again.",
            ["Import.Error.InvalidDateFormat"] = "Invalid date format",
            ["Import.Error.InvalidAmountFormat"] = "Invalid amount format",
            ["Import.Error.Duplicate"] = "Duplicate"
        };

        Dictionary<string, string> spanishTranslations = new Dictionary<string, string>
        {
            ["Import.Error.InvalidExtension"] = "Solo se aceptan archivos Excel (.xls, .xlsx).",
            ["Import.Error.ParseFailed"] = "No se pudo procesar el archivo subido. Comprueba el formato e inténtalo de nuevo.",
            ["Import.Error.InvalidDateFormat"] = "Formato de fecha inválido",
            ["Import.Error.InvalidAmountFormat"] = "Formato de importe inválido",
            ["Import.Error.Duplicate"] = "Duplicado"
        };

        _mockLocalizer
            .Setup(localizer => localizer[It.IsAny<string>()])
            .Returns((string key) =>
            {
                string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                Dictionary<string, string> dictionary = language == "es" ? spanishTranslations : englishTranslations;
                string value = dictionary.GetValueOrDefault(key, key);
                return new LocalizedString(key, value, resourceNotFound: false);
            });
    }

    // ── IH-2a: Happy path ────────────────────────────────────────────────────

    /// <summary>
    /// IH-2a: Two valid Excel rows → two transactions imported, batch metadata saved.
    /// ImportResultDto carries correct counts and filename.
    /// </summary>
    [Fact]
    public async Task Handle_TwoValidRows_ImportsBothAndSavesBatch()
    {
        // Arrange
        var rows = new List<RawTransactionRow>
        {
            new(1, "01/01/2024", null, null, "Coffee shop", null, "-5.50", null),
            new(2, "02/01/2024", null, null, "Salary deposit", null, "2000.00", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), "statement.xlsx"))
            .ReturnsAsync(parseResult);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.TotalProcessed);
        Assert.Equal("statement.xlsx", result.Filename);
        Assert.Empty(result.Errors);
        _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
        _mockImportBatchRepo.Verify(x => x.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<UserId>()), Times.Once);
    }

    // ── IH-2b: Triangulation — empty result ──────────────────────────────────

    /// <summary>
    /// IH-2b: Zero rows from parser → zero transactions imported; batch metadata still saved.
    /// </summary>
    [Fact]
    public async Task Handle_ZeroRows_ImportsNothingButSavesBatch()
    {
        // Arrange
        var parseResult = new StatementParseResult(
            Array.Empty<RawTransactionRow>(),
            Array.Empty<StatementParseRowError>(),
            0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        var command = new ImportTransactionsCommand(new MemoryStream(), "empty.xlsx");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.TotalProcessed);
        Assert.Empty(result.Errors);
        _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _mockImportBatchRepo.Verify(x => x.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<UserId>()), Times.Once);
    }

    // ── IH-2c: DomainException from parser surfaces ───────────────────────────

    /// <summary>
    /// IH-2c: Parser throws DomainException (e.g. missing sheet) → surfaces unchanged.
    /// The message is user-safe and must NOT be replaced.
    /// </summary>
    [Fact]
    public async Task Handle_ParserThrowsDomainException_SurfacesUnchanged()
    {
        // Arrange
        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new DomainException("La hoja 'Movimientos' no fue encontrada."));

        var command = new ImportTransactionsCommand(new MemoryStream(), "bad.xlsx");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        Assert.Contains("Movimientos", ex.Message);
        _mockImportBatchRepo.Verify(x => x.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<UserId>()), Times.Never);
    }

    // ── IH-2d: Triangulation — non-domain infrastructure exception ────────────

    /// <summary>
    /// IH-2d: Parser throws IOException (non-domain) → handler wraps in generic DomainException.
    /// Batch is NOT saved. Sentry capture is a side effect we verify structurally (no crash).
    /// </summary>
    [Fact]
    public async Task Handle_ParserThrowsIoException_ThrowsGenericDomainException()
    {
        // Arrange
        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new IOException("Disk read error"));

        var command = new ImportTransactionsCommand(new MemoryStream(), "corrupt.xls");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        // Message must NOT expose internal infrastructure detail
        Assert.DoesNotContain("Disk read error", ex.Message);
        _mockImportBatchRepo.Verify(x => x.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<UserId>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ParserThrowsIoException_WithSpanishCulture_ThrowsLocalizedGenericDomainException()
    {
        // Arrange
        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new IOException("Disk read error"));

        ImportTransactionsCommand command = new ImportTransactionsCommand(new MemoryStream(), "corrupt.xls");
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("es-ES");

        try
        {
            // Act & Assert
            DomainException ex = await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(command, CancellationToken.None));

            Assert.Equal("No se pudo procesar el archivo subido. Comprueba el formato e inténtalo de nuevo.", ex.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    // ── IH-2e: Invalid file extension ────────────────────────────────────────

    /// <summary>
    /// IH-2e: Uploading a .pdf file → DomainException before parser is ever called.
    /// </summary>
    [Fact]
    public async Task Handle_PdfExtension_ThrowsDomainExceptionBeforeParsing()
    {
        // Arrange
        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        // Parser must NOT have been called
        _mockParser.Verify(
            x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PdfExtension_WithSpanishCulture_ThrowsLocalizedDomainException()
    {
        // Arrange
        ImportTransactionsCommand command = new ImportTransactionsCommand(new MemoryStream(), "statement.pdf");
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("es-ES");

        try
        {
            // Act & Assert
            DomainException ex = await Assert.ThrowsAsync<DomainException>(() =>
                CreateHandler().Handle(command, CancellationToken.None));

            Assert.Equal("Solo se aceptan archivos Excel (.xls, .xlsx).", ex.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    // ── IH-2f: Cross-store duplicate ─────────────────────────────────────────

    /// <summary>
    /// IH-2f: One row that is a cross-store duplicate → skipped+1, error added to result.
    /// Transaction NOT persisted. Batch saved with skippedCount=1.
    /// </summary>
    [Fact]
    public async Task Handle_CrossStoreDuplicate_SkipsAndRecordsError()
    {
        // Arrange
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");

        try
        {
        var rows = new List<RawTransactionRow>
        {
            new(1, "01/01/2024", null, null, "Coffee shop", null, "-5.50", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockTransactionRepo
            .Setup(x => x.ExistsDuplicateAsync(
                It.IsAny<UserId>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<decimal?>()))
            .ReturnsAsync(true); // cross-store duplicate

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Duplicate", result.Errors[0].ErrorMessage);
        _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _mockImportBatchRepo.Verify(x => x.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<UserId>()), Times.Once);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public async Task Handle_CrossStoreDuplicate_WithSpanishCulture_UsesLocalizedDuplicateError()
    {
        // Arrange
        List<RawTransactionRow> rows =
        [
            new(1, "01/01/2024", null, null, "Coffee shop", null, "-5.50", null),
        ];
        StatementParseResult parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        _mockTransactionRepo
            .Setup(x => x.ExistsDuplicateAsync(
                It.IsAny<UserId>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<decimal?>()))
            .ReturnsAsync(true);

        ImportTransactionsCommand command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("es-ES");

        try
        {
            // Act
            ImportResultDto result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            Assert.Single(result.Errors);
            Assert.Equal("Duplicado", result.Errors[0].ErrorMessage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    // ── IH-2g: Parser row errors appear in ImportResultDto.Errors ─────────────

    /// <summary>
    /// IH-2g: Parser returns one RowError (e.g. invalid date) AND one valid row.
    /// → importedCount=1, skippedCount=1, Errors has one entry from RowErrors.
    /// </summary>
    [Fact]
    public async Task Handle_ParserRowError_AppearsInResultErrors()
    {
        // Arrange
        var validRows = new List<RawTransactionRow>
        {
            new(2, "02/01/2024", null, null, "Salary", null, "1000.00", null),
        };
        var rowErrors = new List<StatementParseRowError>
        {
            new(1, "N/A | Coffee | N/A", "Formato de importe inválido: 'N/A'"),
        };
        var parseResult = new StatementParseResult(validRows, rowErrors, 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.Errors);
        Assert.Equal(1, result.Errors[0].RowNumber);
        Assert.Contains("inválido", result.Errors[0].ErrorMessage);
    }

    // ── IH-2h: In-file skipped count (silent, no error entry) ────────────────

    /// <summary>
    /// IH-2h: Parser reports skippedCount=2 (in-file hash dups, spec ESP-3b).
    /// These are silent — no error entries in ImportResultDto.Errors.
    /// </summary>
    [Fact]
    public async Task Handle_InFileSkippedRows_ReflectedInSkippedCountWithNoErrors()
    {
        // Arrange — 0 valid rows, 0 row errors, 2 in-file hash dups
        var parseResult = new StatementParseResult(
            Array.Empty<RawTransactionRow>(),
            Array.Empty<StatementParseRowError>(),
            2); // skippedCount = 2

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — in-file dups counted silently
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(2, result.SkippedCount);
        Assert.Empty(result.Errors); // ESP-3b: hash dups are NOT errors
    }

    // ── IH-2i: Category resolution mapped to Transaction ─────────────────────

    /// <summary>
    /// IH-2i: Row with Category="Compras", SubCategory="Ropa" → ResolveOrCreateAsync called,
    /// resolved IDs and CategorySource=AutoMatched stored on Transaction.
    /// </summary>
    [Fact]
    public async Task Handle_RowWithCategory_ResolutionMappedToTransaction()
    {
        // Arrange
        var categoryId = new CategoryId(Guid.NewGuid());
        var subcategoryId = new SubcategoryId(Guid.NewGuid());

        _mockResolutionService
            .Setup(x => x.ResolveOrCreateAsync(
                It.IsAny<UserId>(),
                It.Is<string?>(v => v == "Compras"),
                It.Is<string?>(v => v == "Ropa"),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolutionResult(categoryId, subcategoryId, CategorySource.AutoMatched));

        var rows = new List<RawTransactionRow>
        {
            new(1, "15/01/2025", "Compras", "Ropa", "Zara Online", null, "-29.95", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        Transaction? captured = null;
        _mockTransactionRepo
            .Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => captured = t)
            .Returns(Task.CompletedTask);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — category resolution data must appear on the stored transaction
        Assert.Equal(1, result.ImportedCount);
        Assert.NotNull(captured);
        Assert.Equal("Compras", captured.BankCategory);
        Assert.Equal("Ropa", captured.BankSubcategory);
        Assert.Equal(categoryId, captured.CategoryId);
        Assert.Equal(subcategoryId, captured.SubcategoryId);
        Assert.Equal(CategorySource.AutoMatched, captured.CategorySource);

        _mockResolutionService.Verify(
            x => x.ResolveOrCreateAsync(
                It.IsAny<UserId>(), "Compras", "Ropa", It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Triangulation: whitespace-padded category trimmed ────────────────────

    /// <summary>
    /// Triangulation: category/subcategory with surrounding whitespace are trimmed
    /// before storing in BankCategory/BankSubcategory.
    /// </summary>
    [Fact]
    public async Task Handle_WhitespacePaddedCategory_TrimmedBeforePersistence()
    {
        // Arrange
        var rows = new List<RawTransactionRow>
        {
            new(1, "01/01/2024", "  Compras  ", " Ropa ", "Coffee", null, "-5.50", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        Transaction? captured = null;
        _mockTransactionRepo
            .Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => captured = t)
            .Returns(Task.CompletedTask);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("Compras", captured.BankCategory);
        Assert.Equal("Ropa", captured.BankSubcategory);
    }

    // ── Both .xls and .xlsx extensions accepted ───────────────────────────────

    /// <summary>
    /// Triangulation: .xls extension (legacy format) must also be accepted.
    /// </summary>
    [Fact]
    public async Task Handle_XlsExtension_AcceptedWithoutException()
    {
        // Arrange
        var parseResult = new StatementParseResult(
            Array.Empty<RawTransactionRow>(),
            Array.Empty<StatementParseRowError>(),
            0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xls");

        // Act — must NOT throw
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        _mockParser.Verify(x => x.ParseAsync(It.IsAny<Stream>(), "statement.xls"), Times.Once);
    }

    // ── Timezone fix: UTC normalization after ParseExact ───────────────────────

    /// <summary>
    /// TZ-1: After ParseExact, the parsed date must be normalized to DateTimeKind.Utc
    /// so it is stored as UTC midnight in the database (TIMESTAMPTZ).
    /// Prevents the timezone bug where Unspecified dates get interpreted as local time.
    /// </summary>
    [Fact]
    public async Task Handle_ParsedDate_NormalizesToUtc()
    {
        // Arrange
        var rows = new List<RawTransactionRow>
        {
            new(1, "15/01/2024", null, null, "Test transaction", null, "-10.00", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        Transaction? captured = null;
        _mockTransactionRepo
            .Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => captured = t)
            .Returns(Task.CompletedTask);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx");

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — date must be UTC Kind
        Assert.NotNull(captured);
        Assert.Equal(DateTimeKind.Utc, captured!.Date.Kind);
        // Date portion must remain the same (15 Jan 2024)
        Assert.Equal(15, captured.Date.Day);
        Assert.Equal(1, captured.Date.Month);
        Assert.Equal(2024, captured.Date.Year);
        Assert.Equal(0, captured.Date.Hour);
    }

    // ── T-PROG-003: progress tracker integration ─────────────────────────────

    /// <summary>
    /// T-PROG-003: When UploadId is provided, the handler initializes progress,
    /// reports progress during row processing, and completes when finished.
    /// </summary>
    [Fact]
    public async Task Handle_WithUploadId_ReportsProgressAndCompletes()
    {
        // Arrange
        var rows = new List<RawTransactionRow>
        {
            new(1, "01/01/2024", null, null, "Coffee shop", null, "-5.50", null),
            new(2, "02/01/2024", null, null, "Salary deposit", null, "2000.00", null),
            new(3, "03/01/2024", null, null, "Grocery", null, "-25.00", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx", "upload-123");

        // Act
        var result = await CreateHandlerWithTracker().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.ImportedCount);
        _mockProgressTracker.Verify(
            x => x.InitializeAsync(
                "upload-123",
                "statement.xlsx",
                3,
                "test-user-id",
                "statement.xlsx",
                1,
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockProgressTracker.Verify(
            x => x.ReportProgressAsync(
                "upload-123",
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _mockProgressTracker.Verify(
            x => x.CompleteAsync(It.IsAny<string>()),
            Times.Never);
        _mockProgressTracker.Verify(
            x => x.FailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// T-PROG-003: When UploadId is null, the tracker must NOT be invoked.
    /// Backward compatibility with existing call sites and tests.
    /// </summary>
    [Fact]
    public async Task Handle_UploadIdNull_DoesNotCallProgressTracker()
    {
        // Arrange
        var rows = new List<RawTransactionRow>
        {
            new(1, "01/01/2024", null, null, "Coffee shop", null, "-5.50", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx", null);

        // Act
        await CreateHandlerWithTracker().Handle(command, CancellationToken.None);

        // Assert
        _mockProgressTracker.Verify(
            x => x.InitializeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _mockProgressTracker.Verify(
            x => x.ReportProgressAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _mockProgressTracker.Verify(
            x => x.CompleteAsync(It.IsAny<string>()),
            Times.Never);
        _mockProgressTracker.Verify(
            x => x.FailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// T-PROG-003: If an exception occurs after progress was initialized,
    /// the exception propagates. The handler does NOT call FailAsync — that
    /// responsibility belongs to the orchestrator (Upload.cshtml.cs).
    /// </summary>
    [Fact]
    public async Task Handle_WithUploadId_ExceptionAfterInit_RethrowsWithoutFailAsync()
    {
        // Arrange
        var rows = new List<RawTransactionRow>
        {
            new(1, "01/01/2024", null, null, "Coffee shop", null, "-5.50", null),
        };
        var parseResult = new StatementParseResult(rows, Array.Empty<StatementParseRowError>(), 0);

        _mockParser
            .Setup(x => x.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(parseResult);
        _mockTransactionRepo
            .Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ThrowsAsync(new InvalidOperationException("Simulated persistence failure"));

        var command = new ImportTransactionsCommand(new MemoryStream(), "statement.xlsx", "upload-fail");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandlerWithTracker().Handle(command, CancellationToken.None));

        Assert.Equal("Simulated persistence failure", ex.Message);
        _mockProgressTracker.Verify(
            x => x.FailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}

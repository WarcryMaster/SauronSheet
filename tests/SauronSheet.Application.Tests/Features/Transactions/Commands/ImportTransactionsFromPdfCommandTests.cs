using SauronSheet.Domain.Common;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Services;
using MediatR;
using Xunit;
using Moq;

namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

public class ImportTransactionsFromPdfCommandTests
{
    private readonly Mock<IPdfParser> _mockPdfParser;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IPdfImportRepository> _mockPdfImportRepo;
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<IBankCategoryResolutionService> _mockResolutionService;

    public ImportTransactionsFromPdfCommandTests()
    {
        _mockPdfParser = new Mock<IPdfParser>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockPdfImportRepo = new Mock<IPdfImportRepository>();
        _mockUserProfileRepo = new Mock<IUserProfileRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockResolutionService = new Mock<IBankCategoryResolutionService>();

        _mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
        _mockUserContext.Setup(x => x.UserEmail).Returns("test@example.com");
        _mockUserProfileRepo.Setup(x => x.EnsureExistsAsync(It.IsAny<UserId>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Default: resolution returns RawOnly (no match found)
        _mockResolutionService
            .Setup(x => x.ResolveAsync(It.IsAny<UserId>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolutionResult(null, null, CategorySource.RawOnly));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_ValidPdf_ImportsTransactions()
    {
        // Arrange
        var handler = new ImportTransactionsFromPdfCommandHandler(
            _mockPdfParser.Object,
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockPdfImportRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object);

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "01/01/2024", null, null, "Coffee", null, "-5.50", null, "EUR"),
            new RawTransactionRow(2, "02/01/2024", null, null, "Salary", null, "2000.00", null, "EUR")
        };

        _mockPdfParser.Setup(x => x.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(rawRows);

        _mockTransactionRepo.Setup(x => x.ExistsDuplicateAsync(
            It.IsAny<UserId>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var stream = new MemoryStream();
        var command = new ImportTransactionsFromPdfCommand(stream, "test.pdf");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.TotalProcessed);
        _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
        _mockPdfImportRepo.Verify(x => x.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<UserId>()), Times.Once);

        // Resolution service called once per row
        _mockResolutionService.Verify(
            x => x.ResolveAsync(It.IsAny<UserId>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_CategoryProvided_ResolutionCalledWithRawValues()
    {
        // Arrange
        var handler = new ImportTransactionsFromPdfCommandHandler(
            _mockPdfParser.Object,
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockPdfImportRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object);

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "01/01/2024", "Compras", "Ropa", "Coffee", null, "-5.50", null, "EUR")
        };

        _mockPdfParser.Setup(x => x.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(rawRows);

        _mockTransactionRepo.Setup(x => x.ExistsDuplicateAsync(
            It.IsAny<UserId>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var stream = new MemoryStream();
        var command = new ImportTransactionsFromPdfCommand(stream, "test.pdf");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — resolution called with raw category/subcategory values
        _mockResolutionService.Verify(
            x => x.ResolveAsync(
                It.IsAny<UserId>(),
                It.Is<string>(v => v == "Compras"),
                It.Is<string>(v => v == "Ropa"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_DuplicateTransactions_SkipsDuplicates()
    {
        // Arrange
        var handler = new ImportTransactionsFromPdfCommandHandler(
            _mockPdfParser.Object,
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockPdfImportRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object);

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "01/01/2024", null, null, "Coffee", null, "-5.50", null, "EUR"),
            new RawTransactionRow(2, "01/01/2024", null, null, "Coffee", null, "-5.50", null, "EUR") // Duplicate
        };

        _mockPdfParser.Setup(x => x.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(rawRows);

        _mockTransactionRepo.SetupSequence(x => x.ExistsDuplicateAsync(
            It.IsAny<UserId>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(false)  // First call: not duplicate
            .ReturnsAsync(true);  // Second call: duplicate

        var stream = new MemoryStream();
        var command = new ImportTransactionsFromPdfCommand(stream, "test.pdf");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Duplicate", result.Errors[0].ErrorMessage);

        // Resolution called only once (for the non-duplicate row)
        _mockResolutionService.Verify(
            x => x.ResolveAsync(It.IsAny<UserId>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_InvalidRows_ReportsErrors()
    {
        // Arrange
        var handler = new ImportTransactionsFromPdfCommandHandler(
            _mockPdfParser.Object,
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockPdfImportRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object);

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "", null, null, "Coffee", null, "-5.50", null, "EUR"), // Missing date
            new RawTransactionRow(2, "2024-01-01", null, null, "", null, "-5.50", null, "EUR"), // Missing description
            new RawTransactionRow(3, "2024-01-01", null, null, "Coffee", null, "", null, "EUR") // Missing amount
        };

        _mockPdfParser.Setup(x => x.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(rawRows);

        var stream = new MemoryStream();
        var command = new ImportTransactionsFromPdfCommand(stream, "test.pdf");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(3, result.SkippedCount);
        Assert.Equal(3, result.Errors.Count);
        Assert.All(result.Errors, error => Assert.Contains("Missing required fields", error.ErrorMessage));

        // Resolution NOT called for invalid rows (skipped before resolution)
        _mockResolutionService.Verify(
            x => x.ResolveAsync(It.IsAny<UserId>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_EmptyPdf_ReturnsZeroCounts()
    {
        // Arrange
        var handler = new ImportTransactionsFromPdfCommandHandler(
            _mockPdfParser.Object,
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockPdfImportRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object);

        _mockPdfParser.Setup(x => x.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new List<RawTransactionRow>());

        var stream = new MemoryStream();
        var command = new ImportTransactionsFromPdfCommand(stream, "empty.pdf");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.TotalProcessed);
        Assert.Empty(result.Errors);

        // Resolution never called for empty PDF
        _mockResolutionService.Verify(
            x => x.ResolveAsync(It.IsAny<UserId>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ImportPdf_ResolutionResult_MapsToTransaction()
    {
        // Arrange
        var handler = new ImportTransactionsFromPdfCommandHandler(
            _mockPdfParser.Object,
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockPdfImportRepo.Object,
            _mockUserProfileRepo.Object,
            _mockUserContext.Object,
            _mockResolutionService.Object);

        var categoryId = new CategoryId(Guid.NewGuid());
        var subcategoryId = new SubcategoryId(Guid.NewGuid());

        // Resolution matches "Compras" → AutoMatched
        _mockResolutionService
            .Setup(x => x.ResolveAsync(
                It.IsAny<UserId>(),
                It.Is<string>(v => v == "Compras"),
                It.Is<string>(v => v == "Ropa"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolutionResult(categoryId, subcategoryId, CategorySource.AutoMatched));

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "01/01/2024", "Compras", "Ropa", "Coffee", null, "-5.50", null, "EUR")
        };

        _mockPdfParser.Setup(x => x.ParseAsync(It.IsAny<Stream>()))
            .ReturnsAsync(rawRows);

        _mockTransactionRepo.Setup(x => x.ExistsDuplicateAsync(
            It.IsAny<UserId>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        Transaction? capturedTransaction = null;
        _mockTransactionRepo
            .Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => capturedTransaction = t)
            .Returns(Task.CompletedTask);

        var stream = new MemoryStream();
        var command = new ImportTransactionsFromPdfCommand(stream, "test.pdf");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — transaction has resolution data
        Assert.NotNull(capturedTransaction);
        Assert.Equal(categoryId, capturedTransaction.CategoryId);
        Assert.Equal(subcategoryId, capturedTransaction.SubcategoryId);
        Assert.Equal("Compras", capturedTransaction.BankCategory);
        Assert.Equal("Ropa", capturedTransaction.BankSubcategory);
        Assert.Equal(CategorySource.AutoMatched, capturedTransaction.CategorySource);
    }
}

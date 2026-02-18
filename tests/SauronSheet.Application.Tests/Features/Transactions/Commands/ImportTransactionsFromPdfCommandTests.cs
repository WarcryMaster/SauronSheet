using Xunit;
using Moq;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Interfaces;
using SauronSheet.Application.Common;
using SauronSheet.Application.Common.Models;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Entities;
using MediatR;

namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

public class ImportTransactionsFromPdfCommandTests
{
    private readonly Mock<IPdfParser> _mockPdfParser;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IPdfImportRepository> _mockPdfImportRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<IMediator> _mockMediator;

    public ImportTransactionsFromPdfCommandTests()
    {
        _mockPdfParser = new Mock<IPdfParser>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockPdfImportRepo = new Mock<IPdfImportRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockMediator = new Mock<IMediator>();

        _mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
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
            _mockUserContext.Object,
            _mockMediator.Object);

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "2024-01-01", "Coffee", "-5.50", "EUR"),
            new RawTransactionRow(2, "2024-01-02", "Salary", "2000.00", "EUR")
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
        _mockPdfImportRepo.Verify(x => x.AddAsync(It.IsAny<ImportBatch>()), Times.Once);
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
            _mockUserContext.Object,
            _mockMediator.Object);

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "2024-01-01", "Coffee", "-5.50", "EUR"),
            new RawTransactionRow(2, "2024-01-01", "Coffee", "-5.50", "EUR") // Duplicate
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
            _mockUserContext.Object,
            _mockMediator.Object);

        var rawRows = new List<RawTransactionRow>
        {
            new RawTransactionRow(1, "", "Coffee", "-5.50", "EUR"), // Missing date
            new RawTransactionRow(2, "2024-01-01", "", "-5.50", "EUR"), // Missing description
            new RawTransactionRow(3, "2024-01-01", "Coffee", "", "EUR") // Missing amount
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
            _mockUserContext.Object,
            _mockMediator.Object);

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
    }
}

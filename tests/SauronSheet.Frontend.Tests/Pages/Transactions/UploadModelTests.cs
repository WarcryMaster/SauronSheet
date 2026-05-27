using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Frontend.Pages.Transactions;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Transactions;

/// <summary>
/// Unit tests for UploadModel — ESP-4 + task 3.3 behavioral contract.
/// RED: reference <see cref="UploadModel.ExcelFile"/> which does not yet exist on the model.
/// GREEN: all pass after UploadModel switches from PdfFile/ImportTransactionsFromPdfCommand
///         to ExcelFile/ImportTransactionsCommand.
/// </summary>
public class UploadModelTests
{
    private readonly Mock<IMediator> _mockMediator;

    public UploadModelTests()
    {
        _mockMediator = new Mock<IMediator>();
    }

    private UploadModel CreateModel() => new(_mockMediator.Object);

    private static Mock<IFormFile> CreateFormFile(string fileName, long sizeBytes = 1024)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(sizeBytes);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[sizeBytes]));
        return mockFile;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Task 3.3 RED: OnPost with null file → generic error (unchanged behavior)
    // This test also exercises the renamed property (ExcelFile).
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_NullFile_SetsGenericErrorMessage()
    {
        // Arrange
        var model = CreateModel();
        model.ExcelFile = null; // ← references ExcelFile (doesn't exist yet → RED compile)

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.NotNull(model.ErrorMessage);
        Assert.Null(model.ImportResult);
        _mockMediator.Verify(m => m.Send(It.IsAny<IRequest<ImportResultDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_EmptyFile_SetsGenericErrorMessage()
    {
        // Arrange
        var model = CreateModel();
        model.ExcelFile = CreateFormFile("statement.xlsx", sizeBytes: 0).Object;

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.NotNull(model.ErrorMessage);
        Assert.Null(model.ImportResult);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Task 3.3 RED: PDF extension must now be REJECTED (currently accepted)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_PdfExtension_SetsExcelOnlyErrorMessage()
    {
        // Arrange
        var model = CreateModel();
        model.ExcelFile = CreateFormFile("statement.pdf").Object;

        // Act
        await model.OnPostAsync();

        // Assert — must mention Excel in the error (not accept PDF)
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Excel", model.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(model.ImportResult);
        _mockMediator.Verify(m => m.Send(It.IsAny<IRequest<ImportResultDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Task 3.3 RED: .xlsx must now be ACCEPTED and dispatch ImportTransactionsCommand
    // (currently rejected because model only accepts .pdf)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_XlsxFile_DispatchesImportTransactionsCommandAndSetsResult()
    {
        // Arrange
        var expectedResult = new ImportResultDto(3, 1, 4, "statement.xlsx", DateTime.UtcNow, []);
        _mockMediator
            .Setup(m => m.Send(
                It.Is<ImportTransactionsCommand>(c => c.Filename == "statement.xlsx"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var model = CreateModel();
        model.ExcelFile = CreateFormFile("statement.xlsx").Object;

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Null(model.ErrorMessage);
        Assert.NotNull(model.ImportResult);
        Assert.Equal(3, model.ImportResult!.ImportedCount);
        _mockMediator.Verify(
            m => m.Send(It.Is<ImportTransactionsCommand>(c => c.Filename == "statement.xlsx"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_XlsFile_DispatchesImportTransactionsCommandAndSetsResult()
    {
        // Arrange — triangulation: .xls (not .xlsx) must also be accepted
        var expectedResult = new ImportResultDto(5, 0, 5, "statement.xls", DateTime.UtcNow, []);
        _mockMediator
            .Setup(m => m.Send(
                It.Is<ImportTransactionsCommand>(c => c.Filename == "statement.xls"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var model = CreateModel();
        model.ExcelFile = CreateFormFile("statement.xls").Object;

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Null(model.ErrorMessage);
        Assert.NotNull(model.ImportResult);
        Assert.Equal(5, model.ImportResult!.ImportedCount);
        _mockMediator.Verify(
            m => m.Send(It.Is<ImportTransactionsCommand>(c => c.Filename == "statement.xls"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_OversizedFile_SetsErrorMessageWithoutDispatching()
    {
        // Arrange — 11 MB > 10 MB limit
        var model = CreateModel();
        model.ExcelFile = CreateFormFile("statement.xlsx", sizeBytes: 11 * 1024 * 1024).Object;

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("10MB", model.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _mockMediator.Verify(m => m.Send(It.IsAny<IRequest<ImportResultDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_DomainExceptionThrown_SetsUserFacingErrorMessage()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<ImportTransactionsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Header row not found in Excel file."));

        var model = CreateModel();
        model.ExcelFile = CreateFormFile("bad.xlsx").Object;

        // Act
        await model.OnPostAsync();

        // Assert — DomainException.Message is user-safe and must appear in ErrorMessage
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains("Header row not found", model.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_GenericExceptionThrown_SetsGenericErrorMessageNotDetails()
    {
        // Arrange — generic exception must NOT leak internal details
        _mockMediator
            .Setup(m => m.Send(It.IsAny<ImportTransactionsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Internal DB connection failed: 10.0.0.1:5432"));

        var model = CreateModel();
        model.ExcelFile = CreateFormFile("statement.xlsx").Object;

        // Act
        await model.OnPostAsync();

        // Assert — generic message; internal details must NOT appear
        Assert.NotNull(model.ErrorMessage);
        Assert.DoesNotContain("10.0.0.1", model.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("connection", model.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}

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
/// Unit tests for UploadModel — multi-file upload support.
/// Tests cover: empty array, single file, multiple files, validation per file,
/// error handling per file, and aggregated results.
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
    // Empty / null file array
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_EmptyFileArray_SetsErrorAndDoesNotDispatch()
    {
        // Arrange
        var model = CreateModel();
        model.ExcelFiles = Array.Empty<IFormFile>();

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Single(model.FileErrors);
        Assert.Contains("at least one", model.FileErrors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Empty(model.ImportResults);
        _mockMediator.Verify(m => m.Send(It.IsAny<IRequest<ImportResultDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Single file — validation
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_SingleEmptyFile_SetsFileError()
    {
        // Arrange
        var model = CreateModel();
        model.ExcelFiles = new[] { CreateFormFile("statement.xlsx", sizeBytes: 0).Object };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Single(model.FileErrors);
        Assert.Contains("empty", model.FileErrors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Empty(model.ImportResults);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_PdfExtension_SetsFileError()
    {
        // Arrange
        var model = CreateModel();
        model.ExcelFiles = new[] { CreateFormFile("statement.pdf").Object };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Single(model.FileErrors);
        Assert.Contains("Excel", model.FileErrors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Empty(model.ImportResults);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_OversizedFile_SetsFileErrorWithoutDispatching()
    {
        // Arrange — 11 MB > 10 MB limit
        var model = CreateModel();
        model.ExcelFiles = new[] { CreateFormFile("statement.xlsx", sizeBytes: 11 * 1024 * 1024).Object };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Single(model.FileErrors);
        Assert.Contains("10MB", model.FileErrors[0], StringComparison.OrdinalIgnoreCase);
        _mockMediator.Verify(m => m.Send(It.IsAny<IRequest<ImportResultDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Single file — success
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_SingleXlsxFile_DispatchesCommandAndAddsResult()
    {
        // Arrange
        var expectedResult = new ImportResultDto(3, 1, 4, "statement.xlsx", DateTime.UtcNow, []);
        _mockMediator
            .Setup(m => m.Send(
                It.Is<ImportTransactionsCommand>(c => c.Filename == "statement.xlsx"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var model = CreateModel();
        model.ExcelFiles = new[] { CreateFormFile("statement.xlsx").Object };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Empty(model.FileErrors);
        Assert.Single(model.ImportResults);
        Assert.Equal(3, model.ImportResults[0].ImportedCount);
        _mockMediator.Verify(
            m => m.Send(It.Is<ImportTransactionsCommand>(c => c.Filename == "statement.xlsx"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_XlsFile_DispatchesCommandAndAddsResult()
    {
        // Arrange
        var expectedResult = new ImportResultDto(5, 0, 5, "statement.xls", DateTime.UtcNow, []);
        _mockMediator
            .Setup(m => m.Send(
                It.Is<ImportTransactionsCommand>(c => c.Filename == "statement.xls"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var model = CreateModel();
        model.ExcelFiles = new[] { CreateFormFile("statement.xls").Object };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Empty(model.FileErrors);
        Assert.Single(model.ImportResults);
        Assert.Equal(5, model.ImportResults[0].ImportedCount);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Multiple files — success
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_MultipleFiles_ProcessesAllAndAggregatesResults()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(
                It.Is<ImportTransactionsCommand>(c => c.Filename == "jan.xlsx"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportResultDto(10, 2, 12, "jan.xlsx", DateTime.UtcNow, []));
        _mockMediator
            .Setup(m => m.Send(
                It.Is<ImportTransactionsCommand>(c => c.Filename == "feb.xlsx"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportResultDto(8, 1, 9, "feb.xlsx", DateTime.UtcNow, []));

        var model = CreateModel();
        model.ExcelFiles = new[]
        {
            CreateFormFile("jan.xlsx").Object,
            CreateFormFile("feb.xlsx").Object
        };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Empty(model.FileErrors);
        Assert.Equal(2, model.ImportResults.Count);
        Assert.Equal(10, model.ImportResults[0].ImportedCount);
        Assert.Equal(8, model.ImportResults[1].ImportedCount);
        _mockMediator.Verify(m => m.Send(It.IsAny<ImportTransactionsCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Multiple files — mixed success and errors
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_MixedValidAndInvalidFiles_ProcessesValidAndCollectsErrors()
    {
        // Arrange — one valid, one bad extension, one oversized
        _mockMediator
            .Setup(m => m.Send(
                It.Is<ImportTransactionsCommand>(c => c.Filename == "good.xlsx"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportResultDto(5, 0, 5, "good.xlsx", DateTime.UtcNow, []));

        var model = CreateModel();
        model.ExcelFiles = new[]
        {
            CreateFormFile("good.xlsx").Object,
            CreateFormFile("bad.pdf").Object,
            CreateFormFile("huge.xlsx", sizeBytes: 11 * 1024 * 1024).Object
        };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Single(model.ImportResults); // only good.xlsx processed
        Assert.Equal(2, model.FileErrors.Count); // bad.pdf + huge.xlsx
        Assert.Contains(model.FileErrors, e => e.Contains("Excel", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(model.FileErrors, e => e.Contains("10MB", StringComparison.OrdinalIgnoreCase));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Error handling per file
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_DomainExceptionThrown_AddsFileErrorWithMessage()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<ImportTransactionsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Header row not found in Excel file."));

        var model = CreateModel();
        model.ExcelFiles = new[] { CreateFormFile("bad.xlsx").Object };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Single(model.FileErrors);
        Assert.Contains("Header row not found", model.FileErrors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPost_GenericExceptionThrown_AddsGenericFileErrorNotDetails()
    {
        // Arrange
        _mockMediator
            .Setup(m => m.Send(It.IsAny<ImportTransactionsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Internal DB connection failed: 10.0.0.1:5432"));

        var model = CreateModel();
        model.ExcelFiles = new[] { CreateFormFile("statement.xlsx").Object };

        // Act
        await model.OnPostAsync();

        // Assert — generic message; internal details must NOT appear
        Assert.Single(model.FileErrors);
        Assert.DoesNotContain("10.0.0.1", model.FileErrors[0], StringComparison.Ordinal);
        Assert.DoesNotContain("connection", model.FileErrors[0], StringComparison.OrdinalIgnoreCase);
    }
}

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using SauronSheet.Application.Resources;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Services;
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
    private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;

    public UploadModelTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
    }

    private UploadModel CreateModel(
        IImportProgressTracker? progressTracker = null,
        string userId = "test-user-id",
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();

        if (userId != null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", userId) },
                "TestAuth"));
        }

        PageContext pageContext = new PageContext(new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));

        UploadModel model = new(_mockMediator.Object, serviceScopeFactory!, _mockLocalizer.Object, progressTracker)
        {
            PageContext = pageContext
        };

        return model;
    }

    private static Mock<IFormFile> CreateFormFile(string fileName, long sizeBytes = 1024)
    {
        Mock<IFormFile> mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(sizeBytes);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[sizeBytes]));
        return mockFile;
    }

    /// <summary>
    /// Creates a mock <see cref="IServiceScopeFactory"/> whose scopes resolve the
    /// given <paramref name="mediator"/> and <paramref name="progressTracker"/>.
    /// Required because <c>OnPostUploadAsync</c> launches background processing
    /// in its own DI scope.
    /// </summary>
    private static Mock<IServiceScopeFactory> CreateMockScopeFactory(
        Mock<IMediator> mediator,
        Mock<IImportProgressTracker> progressTracker)
    {
        Mock<IServiceProvider> mockServiceProvider = new();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IMediator)))
            .Returns(mediator.Object);
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IImportProgressTracker)))
            .Returns(progressTracker.Object);

        Mock<IServiceScope> mockScope = new();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScope.Setup(s => s.Dispose());

        Mock<IServiceScopeFactory> mockFactory = new();
        mockFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        return mockFactory;
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

    // ─────────────────────────────────────────────────────────────────────────────
    // Asynchronous upload endpoint — OnPostUploadAsync
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUploadAsync_ValidFile_ReturnsJsonWithUploadIdAndSuccess()
    {
        // Arrange
        Mock<IImportProgressTracker> mockTracker = new Mock<IImportProgressTracker>();
        Mock<IServiceScopeFactory> mockScopeFactory = CreateMockScopeFactory(_mockMediator, mockTracker);

        UploadModel model = CreateModel(
            mockTracker.Object,
            serviceScopeFactory: mockScopeFactory.Object);
        model.ExcelFiles = new[] { CreateFormFile("statement.xlsx").Object };

        // Act
        IActionResult result = await model.OnPostUploadAsync();

        // Assert — endpoint returns immediately with uploadId.
        // Actual processing happens in a background task (Task.Run + IServiceScopeFactory).
        JsonResult json = Assert.IsType<JsonResult>(result);
        Dictionary<string, object?> values = Assert.IsType<Dictionary<string, object?>>(json.Value);
        Assert.True(values.TryGetValue("uploadId", out object? uploadIdValue));
        Assert.True(values.TryGetValue("success", out object? successValue));
        Assert.True((bool)successValue!);
        Assert.False(string.IsNullOrEmpty((string?)uploadIdValue));

        mockTracker.Verify(t => t.InitializeAsync(
            (string)uploadIdValue!,
            "statement.xlsx",
            0,
            "test-user-id",
            "statement.xlsx",
            1,
            1,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUploadAsync_EmptyFileArray_ReturnsErrorJson()
    {
        // Arrange
        Mock<IImportProgressTracker> mockTracker = new Mock<IImportProgressTracker>();
        UploadModel model = CreateModel(mockTracker.Object);
        model.ExcelFiles = Array.Empty<IFormFile>();

        // Act
        IActionResult result = await model.OnPostUploadAsync();

        // Assert
        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Dictionary<string, object?> values = Assert.IsType<Dictionary<string, object?>>(badRequest.Value!);
        Assert.False((bool)values["success"]!);
        Assert.Contains("at least one", (string?)values["error"], StringComparison.OrdinalIgnoreCase);
        mockTracker.Verify(t => t.InitializeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnPostUploadAsync_InvalidFileExtension_ReturnsErrorJsonWithoutDispatching()
    {
        // Arrange
        Mock<IImportProgressTracker> mockTracker = new Mock<IImportProgressTracker>();
        UploadModel model = CreateModel(mockTracker.Object);
        model.ExcelFiles = new[] { CreateFormFile("statement.pdf").Object };

        // Act
        IActionResult result = await model.OnPostUploadAsync();

        // Assert
        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Dictionary<string, object?> values = Assert.IsType<Dictionary<string, object?>>(badRequest.Value!);
        Assert.False((bool)values["success"]!);
        Assert.Contains("Excel", (string?)values["error"], StringComparison.OrdinalIgnoreCase);
        _mockMediator.Verify(m => m.Send(It.IsAny<ImportTransactionsCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Progress polling endpoint — OnGetProgress
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnGetProgress_WrongUserId_ReturnsForbid()
    {
        // Arrange
        Mock<IImportProgressTracker> mockTracker = new Mock<IImportProgressTracker>();
        ImportProgress progress = new(
            "upload-1", "statement.xlsx", 100, 50, 45, 5, false, false, null,
            "statement.xlsx", 1, 1, "other-user", DateTime.UtcNow, null);

        mockTracker.Setup(t => t.GetProgress("upload-1")).Returns(progress);

        UploadModel model = CreateModel(mockTracker.Object);

        // Act
        IActionResult result = await model.OnGetProgress("upload-1");

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnGetProgress_ValidOwner_ReturnsPartialHtmlWithProgressBar()
    {
        // Arrange
        Mock<IImportProgressTracker> mockTracker = new Mock<IImportProgressTracker>();
        ImportProgress progress = new(
            "upload-1", "statement.xlsx", 200, 50, 45, 5, false, false, null,
            "statement.xlsx", 1, 1, "test-user-id", DateTime.UtcNow, null);

        mockTracker.Setup(t => t.GetProgress("upload-1")).Returns(progress);

        UploadModel model = CreateModel(mockTracker.Object);

        // Act
        IActionResult result = await model.OnGetProgress("upload-1");

        // Assert
        ContentResult content = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html", content.ContentType);
        Assert.Contains("role=\"progressbar\"", content.Content);
        Assert.Contains("aria-valuenow=\"25\"", content.Content);
        Assert.Contains("25%", content.Content);
        Assert.Contains("Processing file 1 of 1: statement.xlsx", content.Content);
        Assert.Contains("50/200 rows", content.Content);
        Assert.Contains("Imported: 45", content.Content);
        Assert.Contains("Skipped: 5", content.Content);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task OnGetProgress_CompletedProgress_SetsStopPollingHeader()
    {
        // Arrange
        Mock<IImportProgressTracker> mockTracker = new Mock<IImportProgressTracker>();
        ImportProgress progress = new(
            "upload-1", "statement.xlsx", 100, 100, 95, 5, true, false, null,
            "statement.xlsx", 1, 1, "test-user-id", DateTime.UtcNow, null);

        mockTracker.Setup(t => t.GetProgress("upload-1")).Returns(progress);

        UploadModel model = CreateModel(mockTracker.Object);

        // Act
        IActionResult result = await model.OnGetProgress("upload-1");

        // Assert
        ContentResult content = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html", content.ContentType);
        Assert.True(model.Response.Headers.ContainsKey("HX-Trigger"));
        Assert.Contains("stopPolling", model.Response.Headers["HX-Trigger"].ToString());
    }
}

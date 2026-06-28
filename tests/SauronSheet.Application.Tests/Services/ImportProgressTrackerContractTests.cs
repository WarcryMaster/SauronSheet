namespace SauronSheet.Application.Tests.Services;

using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Application.Services;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="IImportProgressTracker"/> contract.
/// T-PROG-001: verifies the shape of the interface and that implementations can be invoked.
/// </summary>
[Trait("Category", "Application")]
public class ImportProgressTrackerContractTests
{
    private sealed class FakeTracker : IImportProgressTracker
    {
        public string? LastUploadId { get; private set; }
        public string? LastFilename { get; private set; }
        public int? LastTotalRows { get; private set; }
        public string? LastError { get; private set; }
        public bool Completed { get; private set; }
        public bool Failed { get; private set; }

        public Task InitializeAsync(
            string uploadId,
            string filename,
            int totalRows,
            string userId,
            string currentFileName,
            int currentFileIndex,
            int totalFiles,
            CancellationToken ct)
        {
            LastUploadId = uploadId;
            LastFilename = filename;
            LastTotalRows = totalRows;
            return Task.CompletedTask;
        }

        public Task ReportProgressAsync(
            string uploadId,
            int processedRows,
            int importedCount,
            int skippedCount,
            string? currentFileName,
            CancellationToken ct)
        {
            LastUploadId = uploadId;
            return Task.CompletedTask;
        }

        public Task UpdateCurrentFileAsync(string uploadId, string fileName, int fileIndex, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task CompleteAsync(string uploadId, string? completionDetails = null)
        {
            LastUploadId = uploadId;
            Completed = true;
            return Task.CompletedTask;
        }

        public Task FailAsync(string uploadId, string error)
        {
            LastUploadId = uploadId;
            LastError = error;
            Failed = true;
            return Task.CompletedTask;
        }

        public ImportProgress? GetProgress(string uploadId)
        {
            LastUploadId = uploadId;
            return null;
        }
    }

    [Fact]
    public async Task InitializeAsync_CapturesArguments()
    {
        // Arrange
        IImportProgressTracker tracker = new FakeTracker();

        // Act
        await tracker.InitializeAsync(
            "upload-1",
            "statement.xlsx",
            100,
            "user-1",
            "statement.xlsx",
            1,
            1,
            CancellationToken.None);

        // Assert
        FakeTracker fake = Assert.IsType<FakeTracker>(tracker);
        Assert.Equal("upload-1", fake.LastUploadId);
        Assert.Equal("statement.xlsx", fake.LastFilename);
        Assert.Equal(100, fake.LastTotalRows);
    }

    [Fact]
    public async Task CompleteAsync_And_FailAsync_SetState()
    {
        // Arrange
        IImportProgressTracker tracker = new FakeTracker();

        // Act
        await tracker.CompleteAsync("upload-2");
        await tracker.FailAsync("upload-3", "parse error");

        // Assert
        FakeTracker fake = Assert.IsType<FakeTracker>(tracker);
        Assert.True(fake.Completed);
        Assert.True(fake.Failed);
        Assert.Equal("parse error", fake.LastError);
    }
}

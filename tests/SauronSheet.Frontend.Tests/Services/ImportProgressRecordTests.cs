namespace SauronSheet.Frontend.Tests.Services;

using SauronSheet.Application.Services;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="ImportProgress"/> value record.
/// T-PROG-001: verifies the public property contract used by the real-time upload progress bar.
/// </summary>
[Trait("Category", "Frontend")]
public class ImportProgressRecordTests
{
    [Fact]
    public void Constructor_WithTypicalValues_StoresAllProperties()
    {
        // Arrange
        var startedAt = new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc);

        // Act
        ImportProgress progress = new(
            UploadId: "upload-1",
            Filename: "statement.xlsx",
            TotalRows: 100,
            ProcessedRows: 10,
            ImportedCount: 8,
            SkippedCount: 2,
            IsComplete: false,
            IsFailed: false,
            ErrorMessage: null,
            CurrentFileName: "statement.xlsx",
            CurrentFileIndex: 1,
            TotalFiles: 1,
            UserId: "user-1",
            StartedAt: startedAt);

        // Assert
        Assert.Equal("upload-1", progress.UploadId);
        Assert.Equal("statement.xlsx", progress.Filename);
        Assert.Equal(100, progress.TotalRows);
        Assert.Equal(10, progress.ProcessedRows);
        Assert.Equal(8, progress.ImportedCount);
        Assert.Equal(2, progress.SkippedCount);
        Assert.False(progress.IsComplete);
        Assert.False(progress.IsFailed);
        Assert.Null(progress.ErrorMessage);
        Assert.Equal("statement.xlsx", progress.CurrentFileName);
        Assert.Equal(1, progress.CurrentFileIndex);
        Assert.Equal(1, progress.TotalFiles);
        Assert.Equal("user-1", progress.UserId);
        Assert.Equal(startedAt, progress.StartedAt);
    }

    [Fact]
    public void Constructor_WithFailedState_StoresErrorMessage()
    {
        // Arrange & Act
        ImportProgress progress = new(
            UploadId: "upload-fail",
            Filename: "broken.xls",
            TotalRows: 50,
            ProcessedRows: 5,
            ImportedCount: 0,
            SkippedCount: 5,
            IsComplete: false,
            IsFailed: true,
            ErrorMessage: "Could not parse file.",
            CurrentFileName: "broken.xls",
            CurrentFileIndex: 1,
            TotalFiles: 1,
            UserId: "user-2",
            StartedAt: DateTime.UtcNow);

        // Assert
        Assert.True(progress.IsFailed);
        Assert.Equal("Could not parse file.", progress.ErrorMessage);
    }
}

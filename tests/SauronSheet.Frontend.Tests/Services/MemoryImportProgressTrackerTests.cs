namespace SauronSheet.Frontend.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SauronSheet.Application.Services;
using SauronSheet.Frontend.Services;
using Xunit;

/// <summary>
/// Unit tests for <see cref="MemoryImportProgressTracker"/>.
/// T-PROG-002: verifies IMemoryCache-backed progress storage and thread-safe updates.
/// </summary>
[Trait("Category", "Frontend")]
public class MemoryImportProgressTrackerTests
{
    private static MemoryImportProgressTracker CreateTracker(out IMemoryCache cache)
    {
        cache = new MemoryCache(new MemoryCacheOptions());
        return new MemoryImportProgressTracker(cache);
    }

    [Fact]
    public async Task InitializeAsync_StoresProgressEntry()
    {
        // Arrange
        MemoryImportProgressTracker tracker = CreateTracker(out _);
        DateTime startedAt = DateTime.UtcNow;

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

        ImportProgress? progress = tracker.GetProgress("upload-1");

        // Assert
        Assert.NotNull(progress);
        Assert.Equal("upload-1", progress!.UploadId);
        Assert.Equal("statement.xlsx", progress.Filename);
        Assert.Equal("statement.xlsx", progress.CurrentFileName);
        Assert.Equal(100, progress.TotalRows);
        Assert.Equal(0, progress.ProcessedRows);
        Assert.Equal(0, progress.ImportedCount);
        Assert.Equal(0, progress.SkippedCount);
        Assert.Equal("user-1", progress.UserId);
        Assert.Equal(1, progress.CurrentFileIndex);
        Assert.Equal(1, progress.TotalFiles);
        Assert.False(progress.IsComplete);
        Assert.False(progress.IsFailed);
        Assert.Null(progress.ErrorMessage);
        Assert.True(progress.StartedAt >= startedAt.AddSeconds(-1) && progress.StartedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ReportProgressAsync_UpdatesCounts()
    {
        // Arrange
        MemoryImportProgressTracker tracker = CreateTracker(out _);
        await tracker.InitializeAsync(
            "upload-2",
            "report.xlsx",
            50,
            "user-1",
            "report.xlsx",
            1,
            1,
            CancellationToken.None);

        // Act
        await tracker.ReportProgressAsync(
            "upload-2",
            processedRows: 25,
            importedCount: 20,
            skippedCount: 5,
            ct: CancellationToken.None);

        ImportProgress? progress = tracker.GetProgress("upload-2");

        // Assert
        Assert.NotNull(progress);
        Assert.Equal(25, progress!.ProcessedRows);
        Assert.Equal(20, progress.ImportedCount);
        Assert.Equal(5, progress.SkippedCount);
    }

    [Fact]
    public async Task CompleteAsync_SetsIsComplete()
    {
        // Arrange
        MemoryImportProgressTracker tracker = CreateTracker(out _);
        await tracker.InitializeAsync(
            "upload-3",
            "complete.xlsx",
            10,
            "user-1",
            "complete.xlsx",
            1,
            1,
            CancellationToken.None);
        await tracker.ReportProgressAsync("upload-3", 10, 8, 2, ct: CancellationToken.None);

        // Act
        await tracker.CompleteAsync("upload-3");

        ImportProgress? progress = tracker.GetProgress("upload-3");

        // Assert
        Assert.NotNull(progress);
        Assert.True(progress!.IsComplete);
        Assert.False(progress.IsFailed);
        Assert.Equal(10, progress.ProcessedRows);
        Assert.Equal(8, progress.ImportedCount);
        Assert.Equal(2, progress.SkippedCount);
    }

    [Fact]
    public async Task FailAsync_SetsIsFailedAndErrorMessage()
    {
        // Arrange
        MemoryImportProgressTracker tracker = CreateTracker(out _);
        await tracker.InitializeAsync(
            "upload-4",
            "fail.xlsx",
            10,
            "user-1",
            "fail.xlsx",
            1,
            1,
            CancellationToken.None);

        // Act
        await tracker.FailAsync("upload-4", "Could not parse the uploaded file.");

        ImportProgress? progress = tracker.GetProgress("upload-4");

        // Assert
        Assert.NotNull(progress);
        Assert.True(progress!.IsFailed);
        Assert.False(progress.IsComplete);
        Assert.Equal("Could not parse the uploaded file.", progress.ErrorMessage);
    }

    [Fact]
    public void GetProgress_UnknownUploadId_ReturnsNull()
    {
        // Arrange
        MemoryImportProgressTracker tracker = CreateTracker(out _);

        // Act
        ImportProgress? progress = tracker.GetProgress("unknown-upload");

        // Assert
        Assert.Null(progress);
    }

    [Fact]
    public async Task ConcurrentUpdates_MaintainConsistentState()
    {
        // Arrange
        MemoryImportProgressTracker tracker = CreateTracker(out _);
        const string uploadId = "upload-concurrent";
        const int totalRows = 1000;
        const int workers = 10;
        const int updatesPerWorker = 100;

        await tracker.InitializeAsync(
            uploadId,
            "concurrent.xlsx",
            totalRows,
            "user-1",
            "concurrent.xlsx",
            1,
            1,
            CancellationToken.None);

        int reportCount = 0;
        object countLock = new();

        async Task DoWorkAsync(int workerIndex)
        {
            for (int i = 0; i < updatesPerWorker; i++)
            {
                int processed = (workerIndex * updatesPerWorker) + i + 1;
                await tracker.ReportProgressAsync(
                    uploadId,
                    processed,
                    importedCount: 1,
                    skippedCount: 0,
                    ct: CancellationToken.None);

                lock (countLock)
                {
                    reportCount++;
                }
            }
        }

        List<Task> tasks = new();
        for (int worker = 0; worker < workers; worker++)
        {
            tasks.Add(DoWorkAsync(worker));
        }

        await Task.WhenAll(tasks);

        // Act
        ImportProgress? progress = tracker.GetProgress(uploadId);

        // Assert
        Assert.NotNull(progress);
        Assert.InRange(progress!.ProcessedRows, 1, totalRows);
        Assert.Equal(1, progress.ImportedCount);
        Assert.Equal(0, progress.SkippedCount);
        Assert.Equal(workers * updatesPerWorker, reportCount);
    }
}

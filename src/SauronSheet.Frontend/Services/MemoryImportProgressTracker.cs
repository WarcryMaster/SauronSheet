namespace SauronSheet.Frontend.Services;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Application.Services;

/// <summary>
/// <see cref="IImportProgressTracker"/> implementation backed by <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Thread-safe by design — no external locks needed. Singleton lifetime recommended.
/// </summary>
public sealed class MemoryImportProgressTracker : IImportProgressTracker
{
    private readonly ConcurrentDictionary<string, ImportProgress> _store = new();

    /// <inheritdoc />
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
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        ArgumentException.ThrowIfNullOrEmpty(filename);
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentOutOfRangeException.ThrowIfNegative(totalRows);

        _store[uploadId] = new ImportProgress(
            uploadId,
            filename,
            totalRows,
            0,
            0,
            0,
            false,
            false,
            null,
            currentFileName,
            currentFileIndex,
            totalFiles,
            userId,
            DateTime.UtcNow);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportProgressAsync(
        string uploadId,
        int processedRows,
        int importedCount,
        int skippedCount,
        string? currentFileName = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        SpinWait spin = default;
        while (true)
        {
            if (!_store.TryGetValue(uploadId, out ImportProgress? existing))
                return Task.CompletedTask;

            ImportProgress updated = existing with
            {
                ProcessedRows = processedRows,
                ImportedCount = importedCount,
                SkippedCount = skippedCount,
                CurrentFileName = currentFileName ?? existing.CurrentFileName
            };

            if (_store.TryUpdate(uploadId, updated, existing))
                return Task.CompletedTask;

            spin.SpinOnce();
        }
    }

    /// <inheritdoc />
    public Task UpdateCurrentFileAsync(string uploadId, string fileName, int fileIndex, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        SpinWait spin = default;
        while (true)
        {
            if (!_store.TryGetValue(uploadId, out ImportProgress? existing))
                return Task.CompletedTask;

            ImportProgress updated = existing with
            {
                CurrentFileName = fileName,
                CurrentFileIndex = fileIndex
            };

            if (_store.TryUpdate(uploadId, updated, existing))
                return Task.CompletedTask;

            spin.SpinOnce();
        }
    }

    /// <inheritdoc />
    public Task CompleteAsync(string uploadId)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        SpinWait spin = default;
        while (true)
        {
            if (!_store.TryGetValue(uploadId, out ImportProgress? existing))
                return Task.CompletedTask;

            ImportProgress updated = existing with { IsComplete = true };

            if (_store.TryUpdate(uploadId, updated, existing))
                return Task.CompletedTask;

            spin.SpinOnce();
        }
    }

    /// <inheritdoc />
    public Task FailAsync(string uploadId, string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        ArgumentException.ThrowIfNullOrEmpty(error);

        SpinWait spin = default;
        while (true)
        {
            if (!_store.TryGetValue(uploadId, out ImportProgress? existing))
                return Task.CompletedTask;

            ImportProgress updated = existing with { IsFailed = true, ErrorMessage = error };

            if (_store.TryUpdate(uploadId, updated, existing))
                return Task.CompletedTask;

            spin.SpinOnce();
        }
    }

    /// <inheritdoc />
    public ImportProgress? GetProgress(string uploadId)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        _store.TryGetValue(uploadId, out ImportProgress? progress);
        return progress;
    }
}

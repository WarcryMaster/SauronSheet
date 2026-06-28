namespace SauronSheet.Frontend.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SauronSheet.Application.Services;

/// <summary>
/// <see cref="IImportProgressTracker"/> implementation backed by <see cref="IMemoryCache"/>.
/// Progress entries expire after 5 minutes of inactivity and are guarded by a lock so
/// concurrent updates for the same upload remain consistent.
/// </summary>
public sealed class MemoryImportProgressTracker : IImportProgressTracker, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public MemoryImportProgressTracker(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(
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

        ImportProgress progress = new(
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

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            SetProgress(uploadId, progress);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ReportProgressAsync(
        string uploadId,
        int processedRows,
        int importedCount,
        int skippedCount,
        string? currentFileName = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ImportProgress? existing = GetProgressCore(uploadId);
            if (existing is null)
            {
                return;
            }

            ImportProgress updated = existing with
            {
                ProcessedRows = processedRows,
                ImportedCount = importedCount,
                SkippedCount = skippedCount,
                CurrentFileName = currentFileName ?? existing.CurrentFileName
            };

            SetProgress(uploadId, updated);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateCurrentFileAsync(string uploadId, string fileName, int fileIndex, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ImportProgress? existing = GetProgressCore(uploadId);
            if (existing is null)
            {
                return;
            }

            SetProgress(uploadId, existing with
            {
                CurrentFileName = fileName,
                CurrentFileIndex = fileIndex
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CompleteAsync(string uploadId)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            ImportProgress? existing = GetProgressCore(uploadId);
            if (existing is null)
            {
                return;
            }

            SetProgress(uploadId, existing with { IsComplete = true });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task FailAsync(string uploadId, string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        ArgumentException.ThrowIfNullOrEmpty(error);

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            ImportProgress? existing = GetProgressCore(uploadId);
            if (existing is null)
            {
                return;
            }

            SetProgress(uploadId, existing with { IsFailed = true, ErrorMessage = error });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Reads the current progress snapshot for the given upload.
    /// </summary>
    public ImportProgress? GetProgress(string uploadId)
    {
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        _cache.TryGetValue(BuildCacheKey(uploadId), out ImportProgress? progress);
        return progress;
    }

    private ImportProgress? GetProgressCore(string uploadId)
    {
        _cache.TryGetValue(BuildCacheKey(uploadId), out ImportProgress? progress);
        return progress;
    }

    private void SetProgress(string uploadId, ImportProgress progress)
    {
        MemoryCacheEntryOptions options = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };

        _cache.Set(BuildCacheKey(uploadId), progress, options);
    }

    private static string BuildCacheKey(string uploadId) => $"import-progress-{uploadId}";

    /// <summary>
    /// Releases the synchronization primitive used for thread-safe cache mutations.
    /// </summary>
    public void Dispose() => _lock.Dispose();
}

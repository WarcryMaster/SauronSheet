namespace SauronSheet.Application.Services;

/// <summary>
/// Abstraction for reporting real-time progress of a transaction-import upload.
/// Implementations are responsible for persisting progress so that the frontend
/// can poll it (e.g. via IMemoryCache).
/// </summary>
public interface IImportProgressTracker
{
    /// <summary>
    /// Creates the initial progress entry for an upload.
    /// </summary>
    Task InitializeAsync(
        string uploadId,
        string filename,
        int totalRows,
        string userId,
        string currentFileName,
        int currentFileIndex,
        int totalFiles,
        CancellationToken ct);

    /// <summary>
    /// Updates the processed/imported/skipped counters for an upload.
    /// </summary>
    Task ReportProgressAsync(
        string uploadId,
        int processedRows,
        int importedCount,
        int skippedCount,
        string? currentFileName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks the upload as completed successfully.
    /// </summary>
    Task CompleteAsync(string uploadId);

    /// <summary>
    /// Marks the upload as failed and stores a user-safe error message.
    /// </summary>
    Task FailAsync(string uploadId, string error);

    /// <summary>
    /// Reads the current progress snapshot for the given upload.
    /// </summary>
    ImportProgress? GetProgress(string uploadId);
}

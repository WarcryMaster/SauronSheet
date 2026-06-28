namespace SauronSheet.Application.Services;

/// <summary>
/// Immutable snapshot of an in-progress transaction import.
/// Stored by <see cref="IImportProgressTracker"/> implementations and read by the
/// upload page when polling for progress updates.
/// </summary>
public record ImportProgress(
    string UploadId,
    string Filename,
    int TotalRows,
    int ProcessedRows,
    int ImportedCount,
    int SkippedCount,
    bool IsComplete,
    bool IsFailed,
    string? ErrorMessage,
    string CurrentFileName,
    int CurrentFileIndex,
    int TotalFiles,
    string UserId,
    DateTime StartedAt);

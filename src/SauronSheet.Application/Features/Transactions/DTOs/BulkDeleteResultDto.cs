namespace SauronSheet.Application.Features.Transactions.DTOs;

/// <summary>
/// Response DTO for bulk delete operations.
/// Contains count of deleted transactions, error details if any, and list of failed IDs.
/// </summary>
public record BulkDeleteResultDto(
    int Count,
    string? ErrorMessage,
    IEnumerable<Guid>? FailedTransactionIds = null)
{
    /// <summary>
    /// Number of successfully deleted transactions.
    /// </summary>
    public int Count { get; } = Count;

    /// <summary>
    /// User-friendly error message if deletion failed.
    /// Null if operation succeeded.
    /// </summary>
    public string? ErrorMessage { get; } = ErrorMessage;

    /// <summary>
    /// List of transaction IDs that failed to delete (if partial failure).
    /// Empty if all deleted or all failed atomically.
    /// </summary>
    public IEnumerable<Guid>? FailedTransactionIds { get; } = FailedTransactionIds;
}

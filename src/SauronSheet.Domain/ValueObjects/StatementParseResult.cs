namespace SauronSheet.Domain.ValueObjects;

using System.Collections.Generic;

/// <summary>
/// Result of parsing a bank statement file.
/// Contains successfully parsed rows, per-row errors, and in-file duplicate skip count.
/// </summary>
/// <param name="Rows">Valid transaction rows extracted from the statement.</param>
/// <param name="RowErrors">Per-row errors for rows that could not be parsed (invalid date or amount).</param>
/// <param name="SkippedCount">Number of rows skipped because their content hash was seen earlier in the same file.</param>
public record StatementParseResult(
    IReadOnlyList<RawTransactionRow> Rows,
    IReadOnlyList<StatementParseRowError> RowErrors,
    int SkippedCount
);

/// <summary>
/// Describes a single row that could not be parsed during statement import.
/// </summary>
/// <param name="RowNumber">1-based row number in the source file.</param>
/// <param name="RawContent">Concatenated raw cell values for diagnostics.</param>
/// <param name="Reason">Human-readable reason the row was rejected.</param>
public record StatementParseRowError(
    int RowNumber,
    string RawContent,
    string Reason
);

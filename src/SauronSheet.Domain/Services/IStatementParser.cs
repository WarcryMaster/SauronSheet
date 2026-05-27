namespace SauronSheet.Domain.Services;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ValueObjects;

/// <summary>
/// Bank-agnostic contract for parsing financial statement files.
/// Implementations detect the file format and extract raw transaction rows.
/// Strict validation is applied at the template/header level; row-level failures
/// are reported in <see cref="StatementParseResult.RowErrors"/> without aborting the batch.
/// </summary>
public interface IStatementParser
{
    /// <summary>
    /// Parses the statement stream and returns a <see cref="StatementParseResult"/>
    /// containing valid rows, per-row errors, and the count of in-file duplicates skipped.
    /// </summary>
    /// <param name="stream">Readable stream of the statement file (.xls or .xlsx).</param>
    /// <param name="filename">Original filename — stored on each row as <see cref="RawTransactionRow.ImportedFrom"/>.</param>
    /// <returns>Parse result with valid rows and diagnostics.</returns>
    /// <exception cref="Domain.Exceptions.DomainException">
    /// Thrown when the required sheet is absent or the header row does not match the expected template.
    /// No rows are processed when this exception is raised.
    /// </exception>
    Task<StatementParseResult> ParseAsync(Stream stream, string filename);
}

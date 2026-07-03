namespace SauronSheet.Infrastructure.Excel;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.Extensions.Localization;
using SauronSheet.Application.Resources;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Parses ING bank statement exports in .xls or .xlsx format.
///
/// Template contract (strict, position-based):
///   - Sheet name: "Movimientos"
///   - Row 4 (1-based): header — F. VALOR | CATEGORÍA | SUBCATEGORÍA | DESCRIPCIÓN | COMENTARIO | IMPORTE | SALDO
///   - Data rows: row 5 onward
///
/// COMENTARIO and SALDO are read and discarded — not propagated to the domain model.
/// In-file duplicate detection is based on a content hash of date + amount + description.
/// </summary>
public sealed class IngExcelStatementParser : IStatementParser
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    private static readonly string[] ExpectedHeaders =
    [
        "F. VALOR",
        "CATEGORÍA",
        "SUBCATEGORÍA",
        "DESCRIPCIÓN",
        "COMENTARIO",
        "IMPORTE (€)",   // real ING files: "IMPORTE (€)", not plain "IMPORTE"
        "SALDO (€)"      // real ING files: "SALDO (€)", not plain "SALDO"
    ];

    private const string MovimientosSheet = "Movimientos";
    private const int HeaderRowIndex = 3;     // 0-based → row 4 (1-based)
    private const int DataStartRowIndex = 4;  // 0-based → row 5 (1-based)

    // Column indices (0-based)
    private const int ColFValor = 0;
    private const int ColCategoria = 1;
    private const int ColSubcategoria = 2;
    private const int ColDescripcion = 3;
    private const int ColComentario = 4;  // read & discard
    private const int ColImporte = 5;
    private const int ColSaldo = 6;        // read & discard

    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy"];

    public IngExcelStatementParser(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
    }

    public Task<StatementParseResult> ParseAsync(Stream stream, string filename)
    {
        // Register code pages required for legacy .xls encoding (Windows-1252, ISO-8859-1, etc.)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.GetEncoding(1252)
        });

        // ── Step 1: Locate the "Movimientos" sheet ─────────────────────────────
        bool foundSheet = false;
        do
        {
            if (string.Equals(reader.Name, MovimientosSheet, StringComparison.OrdinalIgnoreCase))
            {
                foundSheet = true;
                break;
            }
        }
        while (reader.NextResult());

        if (!foundSheet)
            throw new DomainException(
                _localizer["Import.Parser.SheetNotFound", MovimientosSheet]);

        // ── Step 2: Read all rows sequentially ────────────────────────────────
        var validRows = new List<RawTransactionRow>();
        var rowErrors = new List<StatementParseRowError>();
        var seenHashes = new HashSet<string>(StringComparer.Ordinal);
        int skippedCount = 0;
        int rowIndex = 0;

        while (reader.Read())
        {
            if (rowIndex == HeaderRowIndex)
            {
                ValidateHeader(reader, _localizer);
            }
            else if (rowIndex >= DataStartRowIndex)
            {
                ProcessDataRow(
                    reader,
                    excelRowNumber: rowIndex + 1,   // convert to 1-based for diagnostics
                    filename,
                    validRows,
                    rowErrors,
                    seenHashes,
                    _localizer,
                    ref skippedCount);
            }

            rowIndex++;
        }

        return Task.FromResult(new StatementParseResult(validRows, rowErrors, skippedCount));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates that the header row exactly matches the expected ING template (by position).
    /// Throws <see cref="DomainException"/> on mismatch.
    /// </summary>
    private static void ValidateHeader(IExcelDataReader reader, IStringLocalizer<SharedResources> localizer)
    {
        for (int i = 0; i < ExpectedHeaders.Length; i++)
        {
            string? actual = reader.FieldCount > i
                ? reader.GetValue(i)?.ToString()?.Trim()
                : null;

            if (!string.Equals(actual, ExpectedHeaders[i], StringComparison.Ordinal))
                throw new DomainException(
                    localizer["Import.Parser.HeaderMismatch", i + 1, ExpectedHeaders[i], actual ?? localizer["Import.Parser.EmptyValue"]]);
        }
    }

    /// <summary>
    /// Processes a single data row. Appends to <paramref name="validRows"/> on success,
    /// to <paramref name="rowErrors"/> on validation failure, or increments <paramref name="skippedCount"/>
    /// for in-file duplicates. Completely empty rows are silently ignored.
    /// </summary>
    private static void ProcessDataRow(
        IExcelDataReader reader,
        int excelRowNumber,
        string filename,
        List<RawTransactionRow> validRows,
        List<StatementParseRowError> rowErrors,
        HashSet<string> seenHashes,
        IStringLocalizer<SharedResources> localizer,
        ref int skippedCount)
    {
        string? rawDate = ReadCellAsString(reader, ColFValor);
        string? rawCategoria = ReadCellAsString(reader, ColCategoria);
        string? rawSubcategoria = ReadCellAsString(reader, ColSubcategoria);
        string? rawDescripcion = ReadCellAsString(reader, ColDescripcion);
        // ColComentario (4) — read and discard
        string? rawImporte = ReadCellAsString(reader, ColImporte);
        string? rawSaldo = ReadCellAsString(reader, ColSaldo);

        // Silently skip completely empty rows (trailing blank rows in the sheet)
        if (string.IsNullOrWhiteSpace(rawDate)
            && string.IsNullOrWhiteSpace(rawDescripcion)
            && string.IsNullOrWhiteSpace(rawImporte))
        {
            return;
        }

        // Validate date — F.VALOR must be a parseable date (raw string preserved for handler)
        if (!IsValidDate(rawDate))
        {
            rowErrors.Add(new StatementParseRowError(
                excelRowNumber,
                $"{rawDate} | {rawDescripcion} | {rawImporte}",
                localizer["Import.Parser.InvalidDate", rawDate ?? string.Empty]));
            return;
        }

        // Validate amount — IMPORTE must be a parseable decimal (raw string preserved for handler)
        if (!IsValidDecimal(rawImporte))
        {
            rowErrors.Add(new StatementParseRowError(
                excelRowNumber,
                $"{rawDate} | {rawDescripcion} | {rawImporte}",
                localizer["Import.Parser.InvalidAmount", rawImporte ?? string.Empty]));
            return;
        }

        // In-file duplicate detection (raw strings used — in-file duplicates are exact copies)
        string hash = ComputeHash(rawDate, rawImporte, rawDescripcion);
        if (!seenHashes.Add(hash))
        {
            skippedCount++;
            return;
        }

        validRows.Add(new RawTransactionRow(
            RowNumber: excelRowNumber,
            Date: rawDate,
            Category: rawCategoria,
            SubCategory: rawSubcategoria,
            Description: rawDescripcion,
            Comment: null,          // COMENTARIO read and discarded per spec ESP-2b
            Amount: rawImporte,
            Balance: rawSaldo,      // SALDO used for duplicate detection
            Currency: "EUR"));
    }

    /// <summary>
    /// Reads a cell value as a trimmed string, handling <see cref="DateTime"/> values
    /// returned by ExcelDataReader for date-formatted cells.
    /// </summary>
    private static string? ReadCellAsString(IExcelDataReader reader, int columnIndex)
    {
        if (reader.FieldCount <= columnIndex)
            return null;

        object? raw = reader.GetValue(columnIndex);

        return raw switch
        {
            null => null,
            DateTime dt => dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            _ => raw.ToString()?.Trim()
        };
    }

    /// <summary>
    /// Returns <c>true</c> if the value can be parsed as a date in the supported ING formats.
    /// </summary>
    private static bool IsValidDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return DateTime.TryParseExact(
            value,
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    /// <summary>
    /// Returns <c>true</c> if the value can be parsed as a decimal number
    /// using invariant (dot) or Spanish (comma) locale.
    /// </summary>
    private static bool IsValidDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            return true;

        if (decimal.TryParse(value, NumberStyles.Any, new CultureInfo("es-ES"), out _))
            return true;

        // Fallback: replace comma with dot
        return decimal.TryParse(
            value.Replace(',', '.').Replace(" ", string.Empty),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out _);
    }

    /// <summary>
    /// Computes a simple content hash string for in-file duplicate detection.
    /// Based on raw date + raw amount + description (case-sensitive).
    /// </summary>
    private static string ComputeHash(string? date, string? amount, string? description)
        => $"{date}|{amount}|{description}";
}

namespace SauronSheet.Infrastructure.Tests.Excel;

using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Infrastructure.Excel;
using Xunit;

/// <summary>
/// Tests for IngExcelStatementParser — covers ESP-1, ESP-2, ESP-3 from spec.
///
/// Scenarios:
///   ESP-1a — Movimientos sheet + valid header → rows parsed from row 5
///   ESP-1b — Sheet absent → DomainException (ParseResult.Error)
///   ESP-1c — Wrong header row 4 → DomainException (ParseResult.Error)
///   ESP-2a — Complete row → ValueDate, Amount, Description, BankCategory, BankSubCategory mapped
///   ESP-2b — COMENTARIO and SALDO present → Comment=null, Balance=null in result
///   ESP-3a — IMPORTE="N/A" → row discarded, RowErrors.Count == 1
///   ESP-3b — Hash duplicate row → SkippedCount == 1
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngExcelStatementParserTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helpers — build minimal .xlsx test fixtures in memory
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a minimal XLSX stream with the correct Movimientos sheet and a valid header.
    /// Rows before the header (rows 1-3) are left empty, header is on row 4, data starts row 5.
    /// </summary>
    private static Stream BuildValidWorkbook(Action<IXLWorksheet> configureData)
    {
        var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Movimientos");

            // Valid header in row 4 (1-based) — matches real ING file format
            ws.Cell(4, 1).Value = "F. VALOR";
            ws.Cell(4, 2).Value = "CATEGORÍA";
            ws.Cell(4, 3).Value = "SUBCATEGORÍA";
            ws.Cell(4, 4).Value = "DESCRIPCIÓN";
            ws.Cell(4, 5).Value = "COMENTARIO";
            ws.Cell(4, 6).Value = "IMPORTE (€)";  // real ING: "IMPORTE (€)", not "IMPORTE"
            ws.Cell(4, 7).Value = "SALDO (€)";    // real ING: "SALDO (€)", not "SALDO"

            configureData(ws);
            wb.SaveAs(ms);
        }

        ms.Position = 0;
        return ms;
    }

    /// <summary>Writes a single data row starting from the given row index (1-based).</summary>
    private static void WriteDataRow(
        IXLWorksheet ws,
        int row,
        string date,
        string category,
        string subCategory,
        string description,
        string comment,
        string amount,
        string balance)
    {
        ws.Cell(row, 1).Value = date;
        ws.Cell(row, 2).Value = category;
        ws.Cell(row, 3).Value = subCategory;
        ws.Cell(row, 4).Value = description;
        ws.Cell(row, 5).Value = comment;
        ws.Cell(row, 6).Value = amount;
        ws.Cell(row, 7).Value = balance;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-1a: Movimientos sheet + valid header → rows parsed from row 5
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_ValidSheetAndHeader_ReturnsRowsFromRow5()
    {
        // Arrange — build a workbook with one valid data row in row 5
        var stream = BuildValidWorkbook(ws =>
        {
            WriteDataRow(ws,
                row: 5,
                date: "15/01/2025",
                category: "Compras",
                subCategory: "Online",
                description: "DAZN",
                comment: "",
                amount: "-12,99",
                balance: "1000,00");
        });

        var parser = new IngExcelStatementParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "test.xlsx");

        // Assert — ESP-1a: exactly one row, no errors, no skips
        Assert.Single(result.Rows);
        Assert.Empty(result.RowErrors);
        Assert.Equal(0, result.SkippedCount);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-1b: Sheet absent → DomainException
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_MovimientosSheetAbsent_ThrowsDomainException()
    {
        // Arrange — workbook with a DIFFERENT sheet name
        var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            wb.AddWorksheet("OtroNombre"); // NOT "Movimientos"
            wb.SaveAs(ms);
        }
        ms.Position = 0;

        var parser = new IngExcelStatementParser();

        // Act & Assert — ESP-1b: DomainException because sheet is absent
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => parser.ParseAsync(ms, "bad-sheet.xlsx"));

        Assert.Contains("Movimientos", ex.Message);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-1c: Wrong header → DomainException
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_WrongHeaderRow4_ThrowsDomainException()
    {
        // Arrange — workbook with "Movimientos" sheet but wrong header in row 4
        var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Movimientos");
            // Wrong header — different column names
            ws.Cell(4, 1).Value = "FECHA";       // should be "F. VALOR"
            ws.Cell(4, 2).Value = "CATEGORIA";   // missing accent
            ws.Cell(4, 3).Value = "SUBCATEGORIA";
            ws.Cell(4, 4).Value = "DESCRIPCION";
            ws.Cell(4, 5).Value = "COMENTARIO";
            ws.Cell(4, 6).Value = "IMPORTE";
            ws.Cell(4, 7).Value = "SALDO";
            wb.SaveAs(ms);
        }
        ms.Position = 0;

        var parser = new IngExcelStatementParser();

        // Act & Assert — ESP-1c: DomainException because header does not match
        await Assert.ThrowsAsync<DomainException>(
            () => parser.ParseAsync(ms, "bad-header.xlsx"));
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-2a: Complete row → fields mapped correctly
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_CompleteRow_MapsAllFieldsCorrectly()
    {
        // Arrange — one complete row per ESP-2a fixture
        var stream = BuildValidWorkbook(ws =>
        {
            WriteDataRow(ws,
                row: 5,
                date: "15/01/2025",
                category: "Compras",
                subCategory: "Online",
                description: "DAZN",
                comment: "algún comentario",
                amount: "-12,99",
                balance: "987,01");
        });

        var parser = new IngExcelStatementParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — ESP-2a: exact field values
        Assert.Single(result.Rows);
        RawTransactionRow row = result.Rows[0];

        Assert.Equal("15/01/2025", row.Date);
        Assert.Equal("-12,99", row.Amount);
        Assert.Equal("DAZN", row.Description);
        Assert.Equal("Compras", row.Category);
        Assert.Equal("Online", row.SubCategory);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-2b: COMENTARIO and SALDO are read and discarded
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_CommentAndBalance_AreDiscardedInResult()
    {
        // Arrange — row with non-empty COMENTARIO and SALDO
        var stream = BuildValidWorkbook(ws =>
        {
            WriteDataRow(ws,
                row: 5,
                date: "20/03/2025",
                category: "Ocio",
                subCategory: "Suscripciones",
                description: "Spotify",
                comment: "esto es un comentario",
                amount: "-9,99",
                balance: "500,00");
        });

        var parser = new IngExcelStatementParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — ESP-2b: Comment and Balance are null in the returned row
        Assert.Single(result.Rows);
        Assert.Null(result.Rows[0].Comment);
        Assert.Null(result.Rows[0].Balance);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-3a: IMPORTE="N/A" → row discarded, RowErrors.Count == 1
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_InvalidAmount_RowIsDiscardedAndErrorRecorded()
    {
        // Arrange — one row with invalid IMPORTE
        var stream = BuildValidWorkbook(ws =>
        {
            WriteDataRow(ws,
                row: 5,
                date: "10/02/2025",
                category: "Varios",
                subCategory: "Otros",
                description: "TRANSFERENCIA",
                comment: "",
                amount: "N/A",   // ← invalid amount per ESP-3a
                balance: "0,00");
        });

        var parser = new IngExcelStatementParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — ESP-3a: row was discarded (Rows empty), error recorded (errors+1)
        Assert.Empty(result.Rows);
        Assert.Single(result.RowErrors);
        Assert.Equal(0, result.SkippedCount);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-3a (triangulation): invalid date also discards row with an error
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_InvalidDate_RowIsDiscardedAndErrorRecorded()
    {
        // Arrange — row with invalid F.VALOR
        var stream = BuildValidWorkbook(ws =>
        {
            WriteDataRow(ws,
                row: 5,
                date: "NOT_A_DATE",  // ← invalid date
                category: "Compras",
                subCategory: "Online",
                description: "Amazon",
                comment: "",
                amount: "-25,00",
                balance: "300,00");
        });

        var parser = new IngExcelStatementParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — invalid date row also triggers an error
        Assert.Empty(result.Rows);
        Assert.Single(result.RowErrors);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-3b: Hash duplicate within file → SkippedCount == 1
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_DuplicateRow_SecondOccurrenceIsSkipped()
    {
        // Arrange — two identical rows in the same file
        var stream = BuildValidWorkbook(ws =>
        {
            WriteDataRow(ws, row: 5,
                date: "01/04/2025", category: "Alimentación", subCategory: "Supermercado",
                description: "MERCADONA", comment: "", amount: "-45,30", balance: "800,00");

            WriteDataRow(ws, row: 6,
                date: "01/04/2025", category: "Alimentación", subCategory: "Supermercado",
                description: "MERCADONA", comment: "", amount: "-45,30", balance: "754,70");
            // NOTE: row 6 is a DUPLICATE of row 5 (same date+amount+description hash)
        });

        var parser = new IngExcelStatementParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — ESP-3b: exactly one row parsed, one skipped
        Assert.Single(result.Rows);
        Assert.Equal(1, result.SkippedCount);
        Assert.Empty(result.RowErrors);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-3b (triangulation): two different rows, nothing skipped
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_TwoDistinctRows_BothAreParsed()
    {
        // Arrange — two rows with different amounts (not duplicates)
        var stream = BuildValidWorkbook(ws =>
        {
            WriteDataRow(ws, row: 5,
                date: "01/04/2025", category: "Alimentación", subCategory: "Supermercado",
                description: "MERCADONA", comment: "", amount: "-45,30", balance: "800,00");

            WriteDataRow(ws, row: 6,
                date: "02/04/2025", category: "Ocio", subCategory: "Streaming",
                description: "NETFLIX", comment: "", amount: "-15,99", balance: "784,01");
        });

        var parser = new IngExcelStatementParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — both rows parsed, zero skipped
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.RowErrors);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ESP-1a (real sample): parse a real ING .xls file without crashing
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParseAsync_RealIngSample_ReturnsNonEmptyResultWithoutCrashing()
    {
        // Arrange — use the real ING sample file (movements-non-2501.xls)
        // The file is copied to the test output via CopyToOutputDirectory in the .csproj
        var samplePath = Path.Combine(AppContext.BaseDirectory, "TestFixtures", "movements-non-2501.xls");
        if (!File.Exists(samplePath))
        {
            // Skip gracefully if fixture file is not available in this environment
            return;
        }

        await using var stream = File.OpenRead(samplePath);
        var parser = new IngExcelStatementParser();

        // Act — must not throw
        StatementParseResult result = await parser.ParseAsync(stream, "movements-non-2501.xls");

        // Assert — real file: at least one row parsed; no catastrophic failures
        Assert.True(result.Rows.Count > 0,
            $"Expected at least one parsed row from real sample. Errors: {result.RowErrors.Count}, Skipped: {result.SkippedCount}");
    }
}

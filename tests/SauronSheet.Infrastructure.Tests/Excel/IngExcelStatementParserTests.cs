namespace SauronSheet.Infrastructure.Tests.Excel;

using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Localization;
using Moq;
using SauronSheet.Application.Resources;
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
    private static IngExcelStatementParser CreateParser()
    {
        Mock<IStringLocalizer<SharedResources>> localizerMock = CreateLocalizerMock();
        return new IngExcelStatementParser(localizerMock.Object);
    }

    private static Mock<IStringLocalizer<SharedResources>> CreateLocalizerMock()
    {
        Dictionary<string, string> englishTranslations = new Dictionary<string, string>
        {
            ["Import.Parser.SheetNotFound"] = "The '{0}' sheet was not found in the file. Verify this is an ING statement with a 'Movimientos' sheet.",
            ["Import.Parser.HeaderMismatch"] = "Invalid header in column {0}: expected '{1}' but found '{2}'.",
            ["Import.Parser.EmptyValue"] = "(empty)",
            ["Import.Parser.InvalidDate"] = "Invalid date format: '{0}'",
            ["Import.Parser.InvalidAmount"] = "Invalid amount format: '{0}'"
        };

        Dictionary<string, string> spanishTranslations = new Dictionary<string, string>
        {
            ["Import.Parser.SheetNotFound"] = "No se encontró la hoja '{0}' en el archivo. Verifica que sea un extracto ING con la hoja 'Movimientos'.",
            ["Import.Parser.HeaderMismatch"] = "Cabecera incorrecta en columna {0}: se esperaba '{1}' pero se encontró '{2}'.",
            ["Import.Parser.EmptyValue"] = "(vacío)",
            ["Import.Parser.InvalidDate"] = "Formato de fecha inválido: '{0}'",
            ["Import.Parser.InvalidAmount"] = "Formato de importe inválido: '{0}'"
        };

        Mock<IStringLocalizer<SharedResources>> localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        localizerMock
            .Setup(localizer => localizer[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) =>
            {
                string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                Dictionary<string, string> dictionary = language == "es" ? spanishTranslations : englishTranslations;
                string format = dictionary.GetValueOrDefault(key, key);
                string value = string.Format(CultureInfo.CurrentCulture, format, args);
                return new LocalizedString(key, value, resourceNotFound: false);
            });

        localizerMock
            .Setup(localizer => localizer[It.IsAny<string>()])
            .Returns((string key) =>
            {
                string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                Dictionary<string, string> dictionary = language == "es" ? spanishTranslations : englishTranslations;
                string value = dictionary.GetValueOrDefault(key, key);
                return new LocalizedString(key, value, resourceNotFound: false);
            });

        return localizerMock;
    }

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

        IngExcelStatementParser parser = CreateParser();

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

        IngExcelStatementParser parser = CreateParser();

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

        IngExcelStatementParser parser = CreateParser();

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

        IngExcelStatementParser parser = CreateParser();

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

        IngExcelStatementParser parser = CreateParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — ESP-2b: Comment is null, Balance is now populated
        Assert.Single(result.Rows);
        Assert.Null(result.Rows[0].Comment);
        Assert.NotNull(result.Rows[0].Balance);
        Assert.Equal("500,00", result.Rows[0].Balance);
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

        IngExcelStatementParser parser = CreateParser();

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

        IngExcelStatementParser parser = CreateParser();

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

        IngExcelStatementParser parser = CreateParser();

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

        IngExcelStatementParser parser = CreateParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "statement.xlsx");

        // Assert — both rows parsed, zero skipped
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.RowErrors);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Golden-fixture tests: real ING .xls file (movements-non-2501.xls)
    //
    // File: src/SauronSheet.Infrastructure/Excel/movements-non-2501.xls
    // Copied to: TestFixtures/movements-non-2501.xls via .csproj CopyToOutputDirectory
    //
    // Ground-truth (produced by parser with CultureInfo.InvariantCulture):
    //   21 valid rows, 0 row-errors, 0 in-file duplicates
    //   Amounts use dot as decimal separator (parser's ReadCellAsString uses InvariantCulture)
    //   COMENTARIO and SALDO always null per spec ESP-2b
    //   Currency always "EUR" (hardcoded in parser)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guard: confirms the CopyToOutputDirectory rule in the .csproj is working.
    /// A failure here means the build target is broken, NOT the parser.
    /// Replacing the old silent "return;" pattern — this test FAILS loudly if the fixture is absent.
    /// </summary>
    [Fact]
    public void RealXlsGolden_FixtureExistsInTestOutputDirectory()
    {
        var samplePath = Path.Combine(AppContext.BaseDirectory, "TestFixtures", "movements-non-2501.xls");

        Assert.True(
            File.Exists(samplePath),
            $"XLS golden fixture is missing from test output. " +
            $"Ensure the .csproj has a CopyToOutputDirectory rule for '*.xls'. " +
            $"Expected path: {samplePath}");
    }

    /// <summary>
    /// ESP-1a (golden): real .xls BIFF format parsed correctly — exactly 21 rows, no errors, no skips.
    /// Verifies the .xls code path (Windows-1252 encoding, BIFF reader) end-to-end.
    /// </summary>
    [Fact]
    public async Task ParseAsync_RealXlsGolden_ParsesExactly21ValidRowsWithNoErrorsOrSkips()
    {
        // Arrange
        var samplePath = Path.Combine(AppContext.BaseDirectory, "TestFixtures", "movements-non-2501.xls");
        Assert.True(File.Exists(samplePath), $"XLS fixture not found: {samplePath}");

        await using var stream = File.OpenRead(samplePath);
        IngExcelStatementParser parser = CreateParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "movements-non-2501.xls");

        // Assert — exact counts from the golden file
        Assert.Equal(21, result.Rows.Count);
        Assert.Empty(result.RowErrors);
        Assert.Equal(0, result.SkippedCount);
    }

    /// <summary>
    /// ESP-2a + ESP-2b (golden, data-driven): every row in the real .xls file has the exact
    /// expected Date, Category, SubCategory, Description, Amount, and the discarded
    /// Comment/Balance are always null per spec ESP-2b.
    /// RowNumber is also verified — data starts at Excel row 5 (1-based), so RowNumber = rowIndex + 5.
    ///
    /// Amounts are dot-separated (InvariantCulture) because ExcelDataReader returns doubles
    /// for numeric cells and the parser calls d.ToString(CultureInfo.InvariantCulture).
    /// </summary>
    [Theory]
    [MemberData(nameof(ExpectedRealXlsRows))]
    public async Task ParseAsync_RealXlsGolden_EachRow_MapsAllFieldsExhaustively(
        int rowIndex,
        int expectedRowNumber,
        string expectedDate,
        string expectedCategory,
        string expectedSubCategory,
        string expectedDescription,
        string expectedAmount)
    {
        // Arrange
        var samplePath = Path.Combine(AppContext.BaseDirectory, "TestFixtures", "movements-non-2501.xls");
        Assert.True(File.Exists(samplePath), $"XLS fixture not found: {samplePath}");

        await using var stream = File.OpenRead(samplePath);
        IngExcelStatementParser parser = CreateParser();

        // Act
        StatementParseResult result = await parser.ParseAsync(stream, "movements-non-2501.xls");

        // Guard — ensure the row index is within the parsed result
        Assert.True(
            rowIndex < result.Rows.Count,
            $"Row index {rowIndex} is out of range. Total parsed rows: {result.Rows.Count}");

        RawTransactionRow row = result.Rows[rowIndex];

        // Assert RowNumber (1-based Excel row number assigned by parser — data starts at row 5)
        Assert.Equal(expectedRowNumber, row.RowNumber);

        // Assert mapped fields (exhaustive per spec ESP-2a)
        Assert.Equal(expectedDate, row.Date);
        Assert.Equal(expectedCategory, row.Category);
        Assert.Equal(expectedSubCategory, row.SubCategory);
        Assert.Equal(expectedDescription, row.Description);
        Assert.Equal(expectedAmount, row.Amount);

        // Assert discarded fields (spec ESP-2b: COMENTARIO always null, SALDO now populated)
        Assert.Null(row.Comment);
        Assert.NotNull(row.Balance);

        // Assert hardcoded currency
        Assert.Equal("EUR", row.Currency);
    }

    /// <summary>
    /// Ground-truth data for the 21 rows in movements-non-2501.xls.
    /// Columns: rowIndex, expectedRowNumber (Excel 1-based, data starts at row 5),
    ///          expectedDate, expectedCategory, expectedSubCategory,
    ///          expectedDescription, expectedAmount.
    /// Amounts use dot separator (parser's InvariantCulture for double cells).
    /// Integer amounts (e.g. "9", "-15", "-45") have no decimal part (ExcelDataReader
    /// returns whole-number doubles whose InvariantCulture ToString omits the decimal).
    /// </summary>
    public static TheoryData<int, int, string, string, string, string, string> ExpectedRealXlsRows =>
        new()
        {
            //  idx  rowNo   date             category                           subCategory                        description                                                                   amount
            {  0,  5,  "31/01/2025", "Otros gastos",                    "Suscripciones",                   "Pago en PAYPAL *DAZN DE 35314369001 DE",                                    "-9.99"   },
            {  1,  6,  "31/01/2025", "Vehículo y transporte",           "Parking y garaje",                "Pago en BSM AP COTXERES DE SARRIABARCELONA ES",                             "-5.65"   },
            {  2,  7,  "31/01/2025", "Nómina y otras prestaciones",     "Nómina o Pensión",                "Nomina recibida MICROSOFT CORPORATION ASSOCIATIONS SAF",                    "3426.05" },
            {  3,  8,  "28/01/2025", "Alimentación",                    "Supermercados y alimentación",    "Pago en MERCADONA FRANCESC MARIMOCOLONIA SEDO ES",                          "-24.38"  },
            {  4,  9,  "26/01/2025", "Compras",                         "Compras (otros)",                 "Pago en AMZN Mktp ES*VU6NZ2T85 800 279 6620 LU",                           "-28.99"  },
            {  5, 10,  "26/01/2025", "Compras",                         "Compras (otros)",                 "Pago en AMZN Mktp ES*L697F2DM5",                                            "-26.03"  },
            {  6, 11,  "25/01/2025", "Otros gastos",                    "Gasto Bizum",                     "Bizum enviado a GERARD MARI PRIETO Comilona",                               "-84.1"   },
            {  7, 12,  "25/01/2025", "Compras",                         "Compras (otros)",                 "Pago en Amazon.es*LZ9RA9NZ5 amazon.esay LU",                                "-20.4"   },
            {  8, 13,  "21/01/2025", "Otros ingresos",                  "Ingresos de otras entidades",     "Transferencia recibida Vinted",                                             "9"       },
            {  9, 14,  "16/01/2025", "Compras",                         "Compras (otros)",                 "Pago Bizum en TULOTERO",                                                    "-15"     },
            { 10, 15,  "13/01/2025", "Compras",                         "Compras (otros)",                 "Pago en AMZN Mktp ES*016ND9IX5",                                            "-47.99"  },
            { 11, 16,  "08/01/2025", "Otros gastos",                    "Gasto Bizum",                     "Bizum enviado a FCO JAVIER DIAZ GONZALEZ Gift",                             "-22.71"  },
            { 12, 17,  "08/01/2025", "Otros ingresos",                  "Ingreso Bizum",                   "Bizum recibido de ALVARO RODRIGO CANTARERO GALVEZ Regalo",                  "13"      },
            { 13, 18,  "07/01/2025", "Otros gastos",                    "Transferencias",                  "Transferencia internacional emitida Movimiento ING",                        "-1200"   },
            { 14, 19,  "06/01/2025", "Ocio y viajes",                   "Cafeterías y restaurantes",       "Pago en MCDONALDS ZARAGOZA ES",                                             "-22.93"  },
            { 15, 20,  "06/01/2025", "Vehículo y transporte",           "Gasolina y combustible",          "Pago en FAMILY ENERGY ZARAGOZA PLATAFORMA LOES",                            "-42.6"   },
            { 16, 21,  "05/01/2025", "Compras",                         "Ropa y complementos",             "Pago en CC. PARQUE CORREDOR TORREJON DE AES",                               "-29.99"  },
            { 17, 22,  "04/01/2025", "Educación y salud",               "Deporte y gimnasio",              "Pago en EASY.TRAININGYM.COM",                                               "-45"     },
            { 18, 23,  "03/01/2025", "Otros ingresos",                  "Ingreso Bizum",                   "Bizum recibido de MARIA ESMERALDA NIETO GARCIA Comida",                     "25.4"    },
            { 19, 24,  "01/01/2025", "Otros gastos",                    "Suscripciones",                   "Pago en PAYPAL *DAZN DE",                                                   "-9.99"   },
            { 20, 25,  "01/01/2025", "Movimientos excluidos",           "Traspaso entre cuentas",          "Traspaso interno emitido periódico SPO To 1727987684",                      "-1000"   },
        };
}

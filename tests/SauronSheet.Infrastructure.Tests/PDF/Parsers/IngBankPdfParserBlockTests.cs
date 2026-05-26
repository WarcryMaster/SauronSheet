namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using System.Collections.Generic;
using System.Reflection;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Integration-unit tests for the block-first pipeline wired inside
/// <see cref="IngBankPdfParser"/>.
///
/// Phase 2 update: ProcessBlocks now accepts an optional IngColumnThresholds parameter.
/// Geometry-based tests use real X-positioned words; text-only tests exercise the
/// conservative fallback (null/null/description) per IBR-3e.
///
/// Covers:
///   Geometry tests    — DAZN, parking, nómina, Bizum, traspaso with real X positions
///                       verify exact PDF literals (PCE-1a through PCE-1d / IBR-3a–3d).
///   Fallback tests    — text-only lines (empty Words[]) → null/null + clean description.
///   Pipeline tests    — monetary extraction, multiline, repeated-header (IBR-4a/4b).
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngBankPdfParserBlockTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Reflection bootstrap — locates ProcessBlocks(IReadOnlyList<IngLineData>, IngColumnThresholds?)
    // Task 2.1 RED: bootstrap fails until task 2.3 adds the second parameter.
    // ────────────────────────────────────────────────────────────────────────

    private static readonly MethodInfo ProcessBlocksMethod;

    static IngBankPdfParserBlockTests()
    {
        ProcessBlocksMethod = typeof(IngBankPdfParser)
            .GetMethod(
                "ProcessBlocks",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(IReadOnlyList<IngLineData>), typeof(IngColumnThresholds)],
                null)
            ?? throw new InvalidOperationException(
                "IngBankPdfParser.ProcessBlocks(IReadOnlyList<IngLineData>, IngColumnThresholds?) " +
                "not found. Task 2.3 (GREEN) must add the thresholds parameter.");
    }

    private static IReadOnlyList<RawTransactionRow> InvokeProcessBlocks(
        IReadOnlyList<IngLineData> dataLines,
        IngColumnThresholds? thresholds = null)
    {
        var result = ProcessBlocksMethod.Invoke(null, [dataLines, thresholds]);
        return (IReadOnlyList<RawTransactionRow>)result!;
    }

    private static IReadOnlyList<IngLineData> StripLeadingRepeatedPageHeaderSection(
        IReadOnlyList<IngLineData> pageLines)
    {
        return IngBankPdfParser.StripLeadingRepeatedPageHeaderSection(pageLines);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Text-only line — no X-position geometry (conservative fallback).</summary>
    private static IngLineData Line(string text)
        => new(text, []);

    /// <summary>Line with real X-positioned words (geometry extraction path).</summary>
    private static IngLineData LineWithWords(string text, params PositionedWord[] words)
        => new(text, words);

    /// <summary>
    /// Calibrated ING Jan-2025 thresholds:
    ///   CategoryStart=175, SubCategoryStart=275, DescriptionStart=375, MonetaryZoneStart=465.
    /// </summary>
    private static IngColumnThresholds FullThresholds()
    {
        var headerWords = new[]
        {
            new PositionedWord("CATEGORÍA",    175.0),
            new PositionedWord("SUBCATEGORÍA", 275.0),
            new PositionedWord("CONCEPTO",     375.0),
            new PositionedWord("IMPORTE",      465.0),
        };
        return IngColumnThresholds.FromHeaderWords(headerWords)!;
    }

    // ════════════════════════════════════════════════════════════════════════
    // GEOMETRY TESTS — exact PDF literals extracted by X-position (PCE-1a–1d)
    // RED: fail until ProcessBlocks wires IngRawColumnExtractor (task 2.3).
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_DaznWithGeometry_ExtractsRawPdfLiterals()
    {
        // Arrange — PCE-1a / IBR-3a: DAZN with real X-positioned words
        var thresholds = FullThresholds();
        var anchorWords = new[]
        {
            new PositionedWord("15/01/2025", 75.0),   // date zone (< CategoryStart 175)
            new PositionedWord("Compras",   180.0),   // category zone [175, 275)
            new PositionedWord("Online",    280.0),   // subcategory zone [275, 375)
            new PositionedWord("DAZN",      380.0),   // description zone [375, 465)
            new PositionedWord("-12,99",    470.0),   // monetary zone ≥465 — excluded
            new PositionedWord("1.234,56",  510.0),   // monetary zone ≥465 — excluded
        };
        var line = LineWithWords(
            "15/01/2025 Compras Online DAZN -12,99 1.234,56", anchorWords);

        // Act
        var rows = InvokeProcessBlocks([line], thresholds);

        // Assert — PCE-1a: exact PDF literals from geometry
        Assert.Single(rows);
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("Compras", rows[0].Category);    // raw PDF literal, not taxonomy
        Assert.Equal("Online",   rows[0].SubCategory); // raw PDF literal
        Assert.Equal("DAZN",     rows[0].Description); // raw PDF literal
        Assert.Equal("-12.99",   rows[0].Amount);
        Assert.Equal("1234.56",  rows[0].Balance);
        Assert.Null(rows[0].Comment); // IBR-2b
    }

    [Fact]
    public void ProcessBlocks_ParkingWithGeometry_ExtractsMultiWordCategoryAndSubcategory()
    {
        // Arrange — PCE-1b / IBR-3b: parking with multi-word raw literals
        var thresholds = FullThresholds();
        var anchorWords = new[]
        {
            new PositionedWord("16/01/2025",  75.0),  // date zone
            new PositionedWord("Vehículo",   180.0),  // category zone
            new PositionedWord("y",          200.0),  // category zone
            new PositionedWord("transporte", 215.0),  // category zone
            new PositionedWord("Parking",    280.0),  // subcategory zone
            new PositionedWord("y",          305.0),  // subcategory zone
            new PositionedWord("garaje",     320.0),  // subcategory zone
            new PositionedWord("Auditorio",  380.0),  // description zone
            new PositionedWord("-3,00",      470.0),  // monetary — excluded
            new PositionedWord("1.231,56",   510.0),  // monetary — excluded
        };
        var line = LineWithWords(
            "16/01/2025 Vehículo y transporte Parking y garaje Auditorio -3,00 1.231,56",
            anchorWords);

        // Act
        var rows = InvokeProcessBlocks([line], thresholds);

        // Assert — PCE-1b: multi-word raw literals from geometry
        Assert.Single(rows);
        Assert.Equal("Vehículo y transporte", rows[0].Category);    // multi-word raw literal
        Assert.Equal("Parking y garaje",      rows[0].SubCategory); // multi-word raw literal
        Assert.Equal("Auditorio",             rows[0].Description);
        Assert.Equal("-3.00",                 rows[0].Amount);
        Assert.Equal("1231.56",               rows[0].Balance);
    }

    [Fact]
    public void ProcessBlocks_NominaWithGeometry_ExtractsNominaLiteralWithNoSubcategory()
    {
        // Arrange — PCE-1c / IBR-3c: payroll — category "Nominas" (raw, not "Nómina"), no subcategory
        var thresholds = FullThresholds();
        var anchorWords = new[]
        {
            new PositionedWord("31/01/2025",  75.0),  // date zone
            new PositionedWord("Nominas",    180.0),  // category zone (raw literal — NOT "Nómina")
            // subcategory zone [275, 375) — no words → SubCategory = null
            new PositionedWord("EMPRESA",    380.0),  // description zone
            new PositionedWord("S.L.",       400.0),  // description zone
            new PositionedWord("2.500,00",   470.0),  // monetary — excluded
            new PositionedWord("3.200,00",   510.0),  // monetary — excluded
        };
        var line = LineWithWords(
            "31/01/2025 Nominas EMPRESA S.L. 2.500,00 3.200,00", anchorWords);

        // Act
        var rows = InvokeProcessBlocks([line], thresholds);

        // Assert — PCE-1c: raw "Nominas" literal, no subcategory
        Assert.Single(rows);
        Assert.Equal("Nominas",       rows[0].Category);    // exact PDF literal
        Assert.Null(rows[0].SubCategory);                   // PCE-1c: no subcategory zone word
        Assert.Equal("EMPRESA S.L.", rows[0].Description);
        Assert.Equal("2500.00",      rows[0].Amount);
        Assert.Equal("3200.00",      rows[0].Balance);
    }

    [Fact]
    public void ProcessBlocks_BizumWithGeometry_ExtractsTransferenciasBizum()
    {
        // Arrange — IBR-3: Bizum transfer with geometry
        var thresholds = FullThresholds();
        var anchorWords = new[]
        {
            new PositionedWord("20/01/2025",      75.0),   // date zone
            new PositionedWord("Transferencias", 180.0),   // category zone
            new PositionedWord("Bizum",          280.0),   // subcategory zone
            new PositionedWord("Pago",           380.0),   // description zone
            new PositionedWord("amigo",          400.0),   // description zone
            new PositionedWord("-50,00",         470.0),   // monetary — excluded
            new PositionedWord("1.181,56",       510.0),   // monetary — excluded
        };
        var line = LineWithWords(
            "20/01/2025 Transferencias Bizum Pago amigo -50,00 1.181,56", anchorWords);

        // Act
        var rows = InvokeProcessBlocks([line], thresholds);

        // Assert — Bizum raw literals from geometry
        Assert.Single(rows);
        Assert.Equal("Transferencias", rows[0].Category);
        Assert.Equal("Bizum",          rows[0].SubCategory);
        Assert.Equal("Pago amigo",     rows[0].Description);
        Assert.Equal("-50.00",         rows[0].Amount);
    }

    [Fact]
    public void ProcessBlocks_TraspasoWithGeometry_ExtractsMisCuentasLiteral()
    {
        // Arrange — PCE-1d / IBR-3d: internal transfer with multi-word category/subcategory
        var thresholds = FullThresholds();
        var anchorWords = new[]
        {
            new PositionedWord("25/01/2025",   75.0),   // date zone
            new PositionedWord("Mis",         180.0),   // category zone
            new PositionedWord("cuentas",     196.0),   // category zone
            new PositionedWord("y",           220.0),   // category zone
            new PositionedWord("depósitos",   235.0),   // category zone
            new PositionedWord("Traspasos",   280.0),   // subcategory zone
            new PositionedWord("propios",     315.0),   // subcategory zone
            new PositionedWord("Cuenta",      380.0),   // description zone
            new PositionedWord("corriente",   402.0),   // description zone
            new PositionedWord("-200,00",     470.0),   // monetary — excluded
            new PositionedWord("5.231,56",    510.0),   // monetary — excluded
        };
        var line = LineWithWords(
            "25/01/2025 Mis cuentas y depósitos Traspasos propios Cuenta corriente -200,00 5.231,56",
            anchorWords);

        // Act
        var rows = InvokeProcessBlocks([line], thresholds);

        // Assert — PCE-1d: multi-word raw literals from geometry
        Assert.Single(rows);
        Assert.Equal("Mis cuentas y depósitos", rows[0].Category);   // exact PDF literal
        Assert.Equal("Traspasos propios",        rows[0].SubCategory); // exact PDF literal
        Assert.Equal("Cuenta corriente",         rows[0].Description);
        Assert.Equal("-200.00",                  rows[0].Amount);
        Assert.Equal("5231.56",                  rows[0].Balance);
    }

    // ════════════════════════════════════════════════════════════════════════
    // FALLBACK TESTS — no geometry (empty Words[]) → IBR-3e conservative null/null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_DaznSingleLine_ReturnsRowWithAmountAndBalance()
    {
        // Arrange — text-only (no positioned words): monetary fields still extracted correctly
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
        ];

        // Act — no thresholds: conservative fallback
        var rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-3e: null/null category; monetary still correct; IBR-2b: null comment
        Assert.Single(rows);
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("-12.99",     rows[0].Amount);
        Assert.Equal("1234.56",    rows[0].Balance);
        Assert.Null(rows[0].Comment);
    }

    [Fact]
    public void ProcessBlocks_ParkingSingleLine_ReturnsRowWithCorrectValues()
    {
        // Arrange — text-only parking row
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("16/01/2025 Ocio Parking Auditorio -3,00 1.231,56"),
        ];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert
        Assert.Single(rows);
        Assert.Equal("16/01/2025", rows[0].Date);
        Assert.Equal("-3.00",      rows[0].Amount);
        Assert.Equal("1231.56",    rows[0].Balance);
        Assert.Null(rows[0].Comment);
    }

    [Fact]
    public void ProcessBlocks_NominaMultiLine_ReturnsOneRowWithJoinedDescription()
    {
        // Arrange — payroll split across two physical lines (text-only)
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Nómina Empresa SA"),
            Line("3.200,00 4.500,00"),
        ];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert — IngBlockAssembler joins lines; monetary extracted from full text
        Assert.Single(rows);
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("3200.00",    rows[0].Amount);
        Assert.Equal("4500.00",    rows[0].Balance);
        Assert.Null(rows[0].Comment);
    }

    [Fact]
    public void ProcessBlocks_MultiLineContinuation_ReturnsOneRowWithBothLinesInDescription()
    {
        // Arrange — description spills onto a second physical line (text-only)
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("20/01/2025 Compras Online DAZN"),
            Line("Suscripción mensual -12,99 1.221,57"),
        ];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert — single row; description includes content from both lines
        Assert.Single(rows);
        Assert.Equal("20/01/2025", rows[0].Date);
        Assert.Equal("-12.99",     rows[0].Amount);
        Assert.Equal("1221.57",    rows[0].Balance);
        Assert.NotNull(rows[0].Description); // description populated from clean text
    }

    [Fact]
    public void ProcessBlocks_NoGeometry_CategoryIsNull()
    {
        // Arrange — IBR-3e: text-only line with valid monetary but no X geometry
        // Previously tested RawOnly fallback (taxonomy 2-token); now verifies IBR-3e null/null.
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("18/01/2025 Inversiones Fondos Pago especial -500,00 731,57"),
        ];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-4b: row is created (not null); IBR-3e: null category (no geometry)
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.NotNull(row);
        Assert.Equal("18/01/2025", row.Date);
        Assert.Equal("-500.00",    row.Amount);
        Assert.Null(row.Category);    // IBR-3e: conservative null — no X geometry
        Assert.Null(row.SubCategory); // IBR-3e: conservative null
    }

    [Fact]
    public void ProcessBlocks_NoMonetaryTokens_BlockSkipped()
    {
        // Arrange — IBR-4a: date but no extractable amount/balance
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("17/01/2025 SomeDescription NoNumbers"),
        ];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-4a: fallback conservador → row not added
        Assert.Empty(rows);
    }

    [Fact]
    public void ProcessBlocks_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        IReadOnlyList<IngLineData> dataLines = [];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert
        Assert.Empty(rows);
    }

    [Fact]
    public void ProcessBlocks_TwoSingleLineRows_ReturnsTwoRows()
    {
        // Arrange — DAZN + Parking back-to-back (text-only)
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
            Line("16/01/2025 Ocio Parking Auditorio -3,00 1.231,56"),
        ];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("16/01/2025", rows[1].Date);
        Assert.Equal("-12.99",     rows[0].Amount);
        Assert.Equal("-3.00",      rows[1].Amount);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Repeated-header and multiline fixes (regressions — IBR-1d / IBR-5)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StripLeadingRepeatedPageHeaderSection_RemovesWholeHeaderSectionBeforeBlockAssembly()
    {
        IReadOnlyList<IngLineData> firstPageTail =
        [
            Line("20/01/2025 Compras Online DAZN"),
        ];

        IReadOnlyList<IngLineData> pageLines =
        [
            Line("Saldo anterior"),
            Line("Página 2 de 3"),
            Line("F. VALOR CATEGORÍA SUBCATEGORÍA CONCEPTO IMPORTE SALDO"),
            Line("Cuenta NARANJA ES12 1234 5678 9012"),
            Line("Resumen de movimientos del periodo"),
            Line("Suscripción mensual -12,99 1.221,57"),
            Line("21/01/2025 Ocio Parking Auditorio -3,00 1.218,57"),
        ];

        var sanitizedPageLines = StripLeadingRepeatedPageHeaderSection(pageLines);
        IReadOnlyList<IngLineData> assembledInput = [.. firstPageTail, .. sanitizedPageLines];

        var rows = InvokeProcessBlocks(assembledInput);

        Assert.Equal(2, rows.Count);
        Assert.Equal("20/01/2025", rows[0].Date);
        Assert.DoesNotContain("Página 2 de 3",    rows[0].Description);
        Assert.DoesNotContain("F. VALOR",          rows[0].Description);
        Assert.DoesNotContain("Cuenta NARANJA",    rows[0].Description);
        Assert.DoesNotContain("Resumen de movimientos del periodo", rows[0].Description);
        Assert.Equal("-12.99",  rows[0].Amount);
        Assert.Equal("1221.57", rows[0].Balance);
        Assert.Equal("21/01/2025", rows[1].Date);
        Assert.Equal("-3.00",   rows[1].Amount);
    }

    [Fact]
    public void StripLeadingRepeatedPageHeaderSection_PreservesTextOnlyContinuationAfterRepeatedHeader()
    {
        IReadOnlyList<IngLineData> firstPageTail =
        [
            Line("20/01/2025 Transferencia recibida"),
        ];

        IReadOnlyList<IngLineData> pageLines =
        [
            Line("Saldo anterior"),
            Line("Página 2 de 3"),
            Line("F. VALOR CATEGORÍA SUBCATEGORÍA CONCEPTO IMPORTE SALDO"),
            Line("Cuenta NARANJA ES12 1234 5678 9012"),
            Line("Resumen de movimientos del periodo"),
            Line("Concepto ampliado de la transferencia"),
            Line("Detalle adicional del emisor"),
            Line("1.500,00 2.000,00"),
            Line("21/01/2025 Ocio Parking Auditorio -3,00 1.997,00"),
        ];

        var sanitizedPageLines = StripLeadingRepeatedPageHeaderSection(pageLines);
        var rows = InvokeProcessBlocks([.. firstPageTail, .. sanitizedPageLines]);

        Assert.Equal(2, rows.Count);
        Assert.Equal("20/01/2025", rows[0].Date);
        Assert.Equal("1500.00",    rows[0].Amount);
        Assert.Equal("2000.00",    rows[0].Balance);

        // IBR-3e: no anchor geometry → null/null (not taxonomy RawOnly anymore)
        Assert.Null(rows[0].Category);
        Assert.Null(rows[0].SubCategory);

        // Description = taxonomyInput (clean text from block, monetary stripped)
        Assert.Contains("Transferencia recibida",                rows[0].Description);
        Assert.Contains("Concepto ampliado de la transferencia", rows[0].Description);
        Assert.Contains("Detalle adicional del emisor",          rows[0].Description);

        // Monetary amounts must NOT appear in description (stripped by IngMonetaryExtractor)
        Assert.DoesNotContain("1.500,00", rows[0].Description ?? string.Empty);
        Assert.DoesNotContain("2.000,00", rows[0].Description ?? string.Empty);
        Assert.DoesNotContain("Página 2 de 3",    rows[0].Description ?? string.Empty);
        Assert.DoesNotContain("F. VALOR",          rows[0].Description ?? string.Empty);
        Assert.DoesNotContain("Cuenta NARANJA",    rows[0].Description ?? string.Empty);

        Assert.Equal("21/01/2025", rows[1].Date);
        Assert.Equal("-3.00",      rows[1].Amount);
    }

    [Fact]
    public void StripLeadingRepeatedPageHeaderSection_PreservesContinuationOnlyPageAfterRepeatedHeader()
    {
        IReadOnlyList<IngLineData> pageLines =
        [
            Line("Saldo anterior"),
            Line("Página 2 de 3"),
            Line("F. VALOR CATEGORÍA SUBCATEGORÍA CONCEPTO IMPORTE SALDO"),
            Line("Cuenta NARANJA ES12 1234 5678 9012"),
            Line("Resumen de movimientos del periodo"),
            Line("Concepto ampliado de la transferencia"),
            Line("Detalle adicional del emisor"),
        ];

        var sanitizedPageLines = StripLeadingRepeatedPageHeaderSection(pageLines);

        Assert.Equal(2, sanitizedPageLines.Count);
        Assert.Equal("Concepto ampliado de la transferencia", sanitizedPageLines[0].Text);
        Assert.Equal("Detalle adicional del emisor",          sanitizedPageLines[1].Text);
    }

    [Fact]
    public void ProcessBlocks_NominaWithAnchor_ProducesCorrectRawTransaction()
    {
        // Arrange — IBR-1d integration: buffer line prepended before nómina anchor
        //   Line 0: DAZN strong anchor → block 0 (clean)
        //   Line 1: "NÓMINA EMPRESA S.L." → ambiguous buffer (after complete block)
        //   Line 2: "31/01/2025 Nominas 2.500,00 3.200,00" → strong anchor → buffer prepended
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
            Line("NÓMINA EMPRESA S.L."),
            Line("31/01/2025 Nominas 2.500,00 3.200,00"),
        ];

        // Act
        var rows = InvokeProcessBlocks(dataLines);

        // Assert — two rows: DAZN preserved + nómina correct
        Assert.Equal(2, rows.Count);

        // Row 0: DAZN block must be clean
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("-12.99",     rows[0].Amount);
        Assert.Equal("1234.56",    rows[0].Balance);
        Assert.DoesNotContain("EMPRESA", rows[0].Description ?? string.Empty, StringComparison.Ordinal);

        // Row 1: nómina — correct amount; IBR-3e: no anchor geometry → null category
        Assert.Equal("31/01/2025", rows[1].Date);
        Assert.Equal("2500.00",    rows[1].Amount);
        Assert.Equal("3200.00",    rows[1].Balance);
        Assert.Null(rows[1].Category);    // IBR-3e: no positioned words → conservative null
        Assert.NotNull(rows[1].Description); // description populated from clean text
    }
}

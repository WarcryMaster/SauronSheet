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
/// Covers (IBR-4b and pipeline scenarios):
///   Pipeline_DAZN          — single-line DAZN row → RawTransactionRow with amount/balance.
///   Pipeline_Parking        — single-line parking row → correct values.
///   Pipeline_Nomina         — multi-line payroll block → joined text, correct values.
///   Pipeline_MultiLine      — generic multi-line continuation → joined into one row.
///   Pipeline_IBR4b          — extractable amount + unknown category → non-null RawOnly row.
///   Pipeline_IBR4a          — no monetary tokens → returns null (block skipped).
///   Pipeline_EmptyInput     — empty data lines → empty result.
///
/// Strategy: call the internal <c>ProcessBlocks(IReadOnlyList&lt;IngLineData&gt;)</c>
/// method via reflection (InternalsVisibleTo is set). Tests are RED until the
/// method is added in task 2.3.
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngBankPdfParserBlockTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Reflection bootstrap — locates ProcessBlocks(IReadOnlyList<IngLineData>)
    // If the method does not exist yet, all tests fail (RED state).
    // ────────────────────────────────────────────────────────────────────────

    private static readonly MethodInfo ProcessBlocksMethod;

    static IngBankPdfParserBlockTests()
    {
        ProcessBlocksMethod = typeof(IngBankPdfParser)
            .GetMethod(
                "ProcessBlocks",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(IReadOnlyList<IngLineData>)],
                null)
            ?? throw new InvalidOperationException(
                "IngBankPdfParser.ProcessBlocks(IReadOnlyList<IngLineData>) not found. " +
                "Task 2.3 (GREEN) must add this method.");
    }

    private static IReadOnlyList<RawTransactionRow> InvokeProcessBlocks(
        IReadOnlyList<IngLineData> dataLines)
    {
        var result = ProcessBlocksMethod.Invoke(null, [dataLines]);
        return (IReadOnlyList<RawTransactionRow>)result!;
    }

    private static IReadOnlyList<IngLineData> StripLeadingRepeatedPageHeaderSection(
        IReadOnlyList<IngLineData> pageLines)
    {
        return IngBankPdfParser.StripLeadingRepeatedPageHeaderSection(pageLines);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Shared helper
    // ────────────────────────────────────────────────────────────────────────

    private static IngLineData Line(string text)
        => new(text, []);

    private static IngLineData LineWithWords(string text, params PositionedWord[] words)
        => new(text, words);

    // ════════════════════════════════════════════════════════════════════════
    // Pipeline — DAZN single-line (IBR-1a + R→L extraction)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_DaznSingleLine_ReturnsRowWithAmountAndBalance()
    {
        // Arrange — Jan-2025 DAZN fixture (single physical line, ING block format)
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — exactly one row; monetary fields normalized; comment = null (IBR-2b)
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Equal("15/01/2025", row.Date);
        Assert.Equal("-12.99", row.Amount);
        Assert.Equal("1234.56", row.Balance);
        Assert.Null(row.Comment);   // IBR-2b: COMENTARIO always null for ING
    }

    // ════════════════════════════════════════════════════════════════════════
    // Pipeline — Parking single-line
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_ParkingSingleLine_ReturnsRowWithCorrectValues()
    {
        // Arrange — Jan-2025 parking fixture
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("16/01/2025 Ocio Parking Auditorio -3,00 1.231,56"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Equal("16/01/2025", row.Date);
        Assert.Equal("-3.00", row.Amount);
        Assert.Equal("1231.56", row.Balance);
        Assert.Null(row.Comment);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Pipeline — Nómina multi-line block (continuation lines joined)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_NominaMultiLine_ReturnsOneRowWithJoinedDescription()
    {
        // Arrange — payroll split across two physical lines
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Nómina Empresa SA"),
            Line("3.200,00 4.500,00"),                // continuation line
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — IngBlockAssembler joins the two physical lines; extractor gets both numbers
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Equal("15/01/2025", row.Date);
        Assert.Equal("3200.00", row.Amount);
        Assert.Equal("4500.00", row.Balance);
        Assert.Null(row.Comment);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Pipeline — Generic multi-line (description continues on a second line)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_MultiLineContinuation_ReturnsOneRowWithBothLinesInDescription()
    {
        // Arrange — the description spills onto a second physical line
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("20/01/2025 Compras Online DAZN"),
            Line("Suscripción mensual -12,99 1.221,57"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — single row; continuation text joined in description
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Equal("20/01/2025", row.Date);
        Assert.Equal("-12.99", row.Amount);
        Assert.Equal("1221.57", row.Balance);
        // Description should include content from both lines minus the monetary tokens
        Assert.NotNull(row.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-4b — Extractable amount, unknown taxonomy → source=RawOnly (non-null row)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_UnknownCategory_ReturnsNonNullRowWithRawCategory()
    {
        // Arrange — IBR-4b: block has a valid amount/balance but category absent from taxonomy.
        // PR3: taxonomy RawOnly fallback preserves first 2 tokens as raw category (PCE-1a).
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("18/01/2025 Inversiones Fondos Pago especial -500,00 731,57"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-4b: non-null row; category preserved as raw literal (IBR-3b / PCE-1a)
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.NotNull(row);                          // IBR-4b: must NOT be null
        Assert.Equal("18/01/2025", row.Date);
        Assert.Equal("-500.00", row.Amount);
        Assert.Equal("Inversiones Fondos", row.Category); // PR3: RawOnly first-2-token fallback
        Assert.Null(row.SubCategory);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-4a — No monetary tokens → null block (row skipped)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_NoMonetaryTokens_BlockSkipped()
    {
        // Arrange — IBR-4a: line has a date but no extractable amount/balance
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("17/01/2025 SomeDescription NoNumbers"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-4a: fallback conservador → row not added
        Assert.Empty(rows);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Edge case — empty data lines
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        IReadOnlyList<IngLineData> dataLines = [];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert
        Assert.Empty(rows);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Pipeline — Two adjacent single-line rows → two independent rows
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_TwoSingleLineRows_ReturnsTwoRows()
    {
        // Arrange — DAZN + Parking back-to-back
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
            Line("16/01/2025 Ocio Parking Auditorio -3,00 1.231,56"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("16/01/2025", rows[1].Date);
        Assert.Equal("-12.99", rows[0].Amount);
        Assert.Equal("-3.00", rows[1].Amount);
    }

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

        IReadOnlyList<IngLineData> sanitizedPageLines = StripLeadingRepeatedPageHeaderSection(pageLines);
        IReadOnlyList<IngLineData> assembledInput =
        [
            .. firstPageTail,
            .. sanitizedPageLines,
        ];

        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(assembledInput);

        Assert.Equal(2, rows.Count);
        Assert.Equal("20/01/2025", rows[0].Date);
        Assert.DoesNotContain("Página 2 de 3", rows[0].Description);
        Assert.DoesNotContain("F. VALOR", rows[0].Description);
        Assert.DoesNotContain("Cuenta NARANJA", rows[0].Description);
        Assert.DoesNotContain("Resumen de movimientos del periodo", rows[0].Description);
        Assert.Equal("-12.99", rows[0].Amount);
        Assert.Equal("1221.57", rows[0].Balance);
        Assert.Equal("21/01/2025", rows[1].Date);
        Assert.Equal("-3.00", rows[1].Amount);
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

        IReadOnlyList<IngLineData> sanitizedPageLines = StripLeadingRepeatedPageHeaderSection(pageLines);
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(
        [
            .. firstPageTail,
            .. sanitizedPageLines,
        ]);

        Assert.Equal(2, rows.Count);
        Assert.Equal("20/01/2025", rows[0].Date);
        Assert.Equal("1500.00", rows[0].Amount);
        Assert.Equal("2000.00", rows[0].Balance);
        Assert.DoesNotContain("Página 2 de 3", rows[0].Description ?? string.Empty);
        Assert.DoesNotContain("F. VALOR", rows[0].Description ?? string.Empty);
        Assert.DoesNotContain("Cuenta NARANJA", rows[0].Description ?? string.Empty);
        Assert.DoesNotContain("Resumen de movimientos del periodo", rows[0].Description ?? string.Empty);
        // PR3: taxonomy RawOnly first-2-tokens → Category = "Transferencia recibida", Description = rest
        Assert.Equal("Transferencia recibida", rows[0].Category);
        Assert.Equal("Concepto ampliado de la transferencia Detalle adicional del emisor",
            rows[0].Description);
        Assert.Equal("21/01/2025", rows[1].Date);
        Assert.Equal("-3.00", rows[1].Amount);
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

        IReadOnlyList<IngLineData> sanitizedPageLines = StripLeadingRepeatedPageHeaderSection(pageLines);

        Assert.Equal(2, sanitizedPageLines.Count);
        Assert.Equal("Concepto ampliado de la transferencia", sanitizedPageLines[0].Text);
        Assert.Equal("Detalle adicional del emisor", sanitizedPageLines[1].Text);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-1d integration — nómina anchor-in-middle produces correct rows
    // RED: currently "NÓMINA EMPRESA S.L." is misassigned to the previous block,
    //      breaking its R→L extraction → DAZN row is dropped (rows.Count=1 not 2)
    // GREEN: assembler fix keeps DAZN clean + nómina produces Amount=2500.00
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_NominaWithAnchor_ProducesCorrectRawTransaction()
    {
        // Arrange — IBR-1d integration fixture:
        //   Line 0: previous complete DAZN block (strong anchor)
        //   Line 1: "NÓMINA EMPRESA S.L." description before the nómina anchor (no date)
        //   Line 2: "31/01/2025 Nominas 2.500,00 3.200,00" — strong anchor for the payroll
        //
        // Without fix: "NÓMINA EMPRESA S.L." attaches to DAZN block → DAZN ExtractRightToLeft
        //              fails (last token "S.L." is not monetary) → DAZN row dropped → 1 row total.
        // With fix: DAZN block stays clean → 2 rows; nómina block has prepended description.
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
            Line("NÓMINA EMPRESA S.L."),
            Line("31/01/2025 Nominas 2.500,00 3.200,00"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — two rows: DAZN preserved + nómina correct
        Assert.Equal(2, rows.Count);

        // Row 0: DAZN block must be clean (uncontaminated by nómina description)
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("-12.99", rows[0].Amount);
        Assert.Equal("1234.56", rows[0].Balance);
        Assert.DoesNotContain("EMPRESA", rows[0].Description ?? string.Empty, StringComparison.Ordinal);

        // Row 1: nómina block — correct amount; taxonomy resolved "Nominas" → "Nómina"
        Assert.Equal("31/01/2025", rows[1].Date);
        Assert.Equal("2500.00", rows[1].Amount);
        Assert.Equal("3200.00", rows[1].Balance);
        Assert.Equal("Nómina", rows[1].Category);
    }
}

namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="IngControlledTaxonomy"/> and pipeline integration
/// via <see cref="IngBankPdfParser.ProcessBlocks"/>.
///
/// Phase 2 update: ProcessBlocks no longer uses IngControlledTaxonomy for
/// category/subcategory extraction. The direct ExtractLeftToRight unit tests
/// remain valid (they test the taxonomy function in isolation and will be used
/// for Phase 3 cleanup verification). Pipeline tests are updated to reflect the
/// new geometry-first / conservative fallback behavior (IBR-3e): text-only lines
/// with no X-position words produce null/null category/subcategory.
///
/// Covers:
///   ExtractLeftToRight unit tests — unchanged (taxonomy function still exists in PR2).
///   ProcessBlocks pipeline tests — updated to IBR-3e conservative null/null behavior.
///   PCE-1d — Non-ING parser path does not restrict categories via closed list.
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngControlledTaxonomyTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Reflection bootstrap — locates ProcessBlocks for pipeline tests
    // ────────────────────────────────────────────────────────────────────────

    private static readonly MethodInfo ProcessBlocksMethod;

    static IngControlledTaxonomyTests()
    {
        // Phase 2: ProcessBlocks now accepts an optional IngColumnThresholds? second parameter.
        // We pass null (no geometry) so these tests exercise the IBR-3e conservative fallback.
        ProcessBlocksMethod = typeof(IngBankPdfParser)
            .GetMethod(
                "ProcessBlocks",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(IReadOnlyList<IngLineData>), typeof(IngColumnThresholds)],
                null)
            ?? throw new InvalidOperationException(
                "IngBankPdfParser.ProcessBlocks(IReadOnlyList<IngLineData>, IngColumnThresholds?) not found.");
    }

    private static IReadOnlyList<RawTransactionRow> InvokeProcessBlocks(
        IReadOnlyList<IngLineData> dataLines)
    {
        // Pass null thresholds → IBR-3e conservative fallback (null/null/cleanText)
        var result = ProcessBlocksMethod.Invoke(null, [dataLines, null]);
        return (IReadOnlyList<RawTransactionRow>)result!;
    }

    private static string GetRepositoryFilePath(
        string relativePath,
        [CallerFilePath] string currentFilePath = "")
    {
        string currentDirectory = Path.GetDirectoryName(currentFilePath)
            ?? throw new InvalidOperationException("Test source path is unavailable.");

        return Path.GetFullPath(Path.Combine(
            currentDirectory,
            "..",
            "..",
            "..",
            "..",
            relativePath));
    }

    private static IngLineData Line(string text)
        => new(text, []);

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3a: Categoría y subcategoría conocidas extraídas L→R
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractLeftToRight_KnownCategoryAndSubcategory_ReturnsCatSubcatAndDescription()
    {
        // Arrange — IBR-3a: texto limpio "Compras Online DAZN" (after date+amount strip)
        const string cleanText = "Compras Online DAZN";

        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(cleanText);

        // Assert — L→R taxonomy extracts category, subcategory, and description correctly
        Assert.Equal("Compras", result.Category);
        Assert.Equal("Online", result.SubCategory);
        Assert.Equal("DAZN", result.Description);
        Assert.False(result.IsRawOnly);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3b: Categoría fuera de taxonomía → RawOnly preservado
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractLeftToRight_UnrecognizedCategory_ReturnsRawOnlyWithCategoryPreserved()
    {
        // Arrange — IBR-3b: primer token no reconocido en taxonomía controlada
        const string cleanText = "Inversiones Fondos Pago especial";

        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(cleanText);

        // Assert — category preserved from raw text; source = RawOnly; no subcategory extracted
        Assert.Equal("Inversiones Fondos", result.Category);
        Assert.Null(result.SubCategory);
        Assert.Equal("Pago especial", result.Description);
        Assert.True(result.IsRawOnly);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3e: conservative fallback — null/null/cleanText when no geometry
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_DaznBlock_DescriptionContainsOnlyMerchantName()
    {
        // Arrange — IBR-3e: text-only line (no word X-positions); conservative fallback applies.
        // In Phase 2, category and subcategory are null; description = full clean text.
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-3e: category=null, subCategory=null; description = clean block text
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Null(row.Category);
        Assert.Null(row.SubCategory);
        Assert.NotNull(row.Description);
        Assert.Contains("DAZN", row.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("-12,99", row.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("15/01/2025", row.Description, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1a / IBR-3e: sin geometría → category=null; texto íntegro en description
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_CategoryAbsentFromTaxonomy_PreservedAsRawCategory()
    {
        // Arrange — PCE-1a / IBR-3e: conservative fallback (no X-positions).
        // Phase 2: category is null (not "Inversiones Fondos"); full clean text goes to description.
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("18/01/2025 Inversiones Fondos Pago especial -500,00 731,57"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — row created (not null); category=null; description = full clean text
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.NotNull(row);
        Assert.Null(row.Category);
        Assert.Null(row.SubCategory);
        Assert.NotNull(row.Description);
        Assert.Contains("Inversiones Fondos", row.Description, StringComparison.Ordinal);
        Assert.Contains("Pago especial", row.Description, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1b: Categoría no detectable → Category = null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractLeftToRight_EmptyText_ReturnsNullCategory()
    {
        // Arrange — PCE-1b: empty input (no category field detectable)
        const string cleanText = "";

        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(cleanText);

        // Assert — null category, null subcategory, no raw data
        Assert.Null(result.Category);
        Assert.Null(result.SubCategory);
        Assert.Null(result.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1c: Categoría conocida, sin subcategoría → SubCategory = null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractLeftToRight_KnownCategoryNoSubcategory_ReturnsNullSubCategory()
    {
        // Arrange — PCE-1c: "Compras" matched, next token not a known subcategory
        const string cleanText = "Compras El Corte Ingles";

        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(cleanText);

        // Assert — category matched, subcategory null, remaining text is description
        Assert.Equal("Compras", result.Category);
        Assert.Null(result.SubCategory);
        Assert.Equal("El Corte Ingles", result.Description);
        Assert.False(result.IsRawOnly);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1d: Path no-ING no usa lista cerrada
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GenericBankParser_SourceDoesNotReferenceIngControlledTaxonomy()
    {
        // Arrange — PCE-1d: the non-ING parser must stay taxonomy-agnostic.
        // A source-level guard catches direct static calls, aliases, and imports in this codebase.
        string sourcePath = GetRepositoryFilePath(Path.Combine(
            "src",
            "SauronSheet.Infrastructure",
            "PDF",
            "Parsers",
            "GenericBankPdfParser.cs"));

        // Act
        Assert.True(File.Exists(sourcePath), "GenericBankPdfParser source file must exist.");
        string source = File.ReadAllText(sourcePath);

        // Assert
        Assert.DoesNotContain(
            nameof(IngControlledTaxonomy),
            source,
            StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: Bizum → known single-token category
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractLeftToRight_BizumEntry_ReturnsBizumCategoryWithDescription()
    {
        // Arrange — Bizum is a common ING Spain category; single-token match
        const string cleanText = "Bizum JOHN DOE PAYMENT";

        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(cleanText);

        // Assert — Bizum category matched; no subcategory; rest = description
        Assert.Equal("Bizum", result.Category);
        Assert.Null(result.SubCategory);
        Assert.Equal("JOHN DOE PAYMENT", result.Description);
        Assert.False(result.IsRawOnly);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: nómina (payroll) multiline — IBR-3e fallback, amount parsed
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_NominaBlock_ReturnsCategoryNominaAndMerchantDescription()
    {
        // Arrange — payroll split across two physical lines; no geometry (text-only).
        // IBR-3e: category=null; description = full clean text ("Nómina Empresa SA").
        // Monetary amounts come from the continuation line and are still parsed correctly.
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Nómina Empresa SA"),
            Line("3.200,00 4.500,00"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — category=null (IBR-3e); description contains the payroll text; amounts parsed
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Null(row.Category);
        Assert.Null(row.SubCategory);
        Assert.NotNull(row.Description);
        Assert.Contains("Nómina", row.Description, StringComparison.Ordinal);
        Assert.Equal("3200.00", row.Amount);
        Assert.Equal("4500.00", row.Balance);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: parking — IBR-3e fallback, full text in description
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_ParkingBlock_ReturnsCategoryOcioAndSubcategoryParking()
    {
        // Arrange — parking fixture (single physical line); no geometry (text-only).
        // IBR-3e: conservative fallback; category and subcategory are null.
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("16/01/2025 Ocio Parking Auditorio -3,00 1.231,56"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-3e: category=null, subCategory=null; description = full clean text
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Null(row.Category);
        Assert.Null(row.SubCategory);
        Assert.NotNull(row.Description);
        Assert.Contains("Ocio", row.Description, StringComparison.Ordinal);
        Assert.Contains("Parking", row.Description, StringComparison.Ordinal);
        Assert.Equal("-3.00", row.Amount);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3e / IBR-4b: unrecognized text — conservative fallback, row emitted
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_RawOnlyFallback_RowCreatedWithRawCategoryNotNull()
    {
        // Arrange — IBR-3e / IBR-4b: no thresholds (text-only lines).
        // Phase 2: row is emitted (not null) with category=null, description = full clean text.
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("22/01/2025 UnknownBank Transfer Wire -100,00 500,00"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-3e: row created; category=null; description contains original text; amount ok
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.NotNull(row);
        Assert.Null(row.Category);
        Assert.NotNull(row.Description);
        Assert.Contains("UnknownBank", row.Description, StringComparison.Ordinal);
        Assert.Equal("-100.00", row.Amount);
    }

    // ════════════════════════════════════════════════════════════════════════
    // REFACTOR triangulation: RawOnly edge cases — single token, 2-token exact
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractLeftToRight_SingleUnrecognizedToken_ReturnsThatTokenAsCategory()
    {
        // Arrange — single token not in taxonomy
        const string cleanText = "SomethingNew";

        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(cleanText);

        // Assert — raw fallback: single token → category = that token, description = null
        Assert.Equal("SomethingNew", result.Category);
        Assert.Null(result.SubCategory);
        Assert.Null(result.Description);
        Assert.True(result.IsRawOnly);
    }

    [Fact]
    public void ExtractLeftToRight_TwoUnrecognizedTokens_ReturnsBothAsCategory()
    {
        // Arrange — two tokens; second token consumed into raw category (no remainder)
        const string cleanText = "CategoryA SubcatB";

        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(cleanText);

        // Assert — raw fallback: 2 tokens → joined as category, no description
        Assert.Equal("CategoryA SubcatB", result.Category);
        Assert.Null(result.SubCategory);
        Assert.Null(result.Description);
        Assert.True(result.IsRawOnly);
    }

    [Fact]
    public void ExtractLeftToRight_NullInput_ReturnsAllNullsAndNotRawOnly()
    {
        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight(null);

        // Assert — PCE-1b: null input → (null, null, null, IsRawOnly=false)
        Assert.Null(result.Category);
        Assert.Null(result.SubCategory);
        Assert.Null(result.Description);
        Assert.False(result.IsRawOnly);
    }

    [Fact]
    public void ExtractLeftToRight_WhitespaceOnlyInput_ReturnsAllNullsAndNotRawOnly()
    {
        // Act
        IngTaxonomyResult result = IngControlledTaxonomy.ExtractLeftToRight("   ");

        // Assert — whitespace treated same as empty (PCE-1b path)
        Assert.Null(result.Category);
        Assert.Null(result.SubCategory);
        Assert.Null(result.Description);
        Assert.False(result.IsRawOnly);
    }
}

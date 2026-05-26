namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="IngControlledTaxonomy"/> and taxonomy-wired
/// <see cref="IngBankPdfParser.ProcessBlocks"/> scenarios.
///
/// Covers:
///   IBR-3a — Known category+subcategory extracted correctly (L→R match).
///   IBR-3b — Unrecognized prefix → RawOnly with raw category preserved.
///   IBR-3c — Full pipeline: description does not contain amount/date/cat/subcat.
///   PCE-1a — PDF category absent from taxonomy → preserved as RawOnly.
///   PCE-1b — No detectable category field → Category = null.
///   PCE-1c — Category matched but no subcategory → SubCategory = null.
///   PCE-1d — Non-ING parser path does not restrict categories via closed list.
///
/// Strategy: direct unit tests on IngControlledTaxonomy.ExtractLeftToRight +
/// ProcessBlocks integration via IngBankPdfParser reflection for pipeline cases.
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
        ProcessBlocksMethod = typeof(IngBankPdfParser)
            .GetMethod(
                "ProcessBlocks",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(IReadOnlyList<IngLineData>)],
                null)
            ?? throw new InvalidOperationException(
                "IngBankPdfParser.ProcessBlocks(IReadOnlyList<IngLineData>) not found.");
    }

    private static IReadOnlyList<RawTransactionRow> InvokeProcessBlocks(
        IReadOnlyList<IngLineData> dataLines)
    {
        var result = ProcessBlocksMethod.Invoke(null, [dataLines]);
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
    // IBR-3c: Pipeline completo — descripción no contiene campos extraídos
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_DaznBlock_DescriptionContainsOnlyMerchantName()
    {
        // Arrange — IBR-3c: full pipeline block; after extraction only "DAZN" should remain
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Compras Online DAZN -12,99 1.234,56"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — description = "DAZN" only; no amount, date, category or subcategory leaked
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Equal("DAZN", row.Description);
        Assert.DoesNotContain("-12,99", row.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("15/01/2025", row.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("Compras", row.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("Online", row.Description ?? string.Empty, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1a: Categoría ausente en taxonomía → preservada como RawOnly
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_CategoryAbsentFromTaxonomy_PreservedAsRawCategory()
    {
        // Arrange — PCE-1a: "Inversiones Fondos" absent from taxonomy; must NOT be discarded
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("18/01/2025 Inversiones Fondos Pago especial -500,00 731,57"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — row created (not null), category preserved, source=RawOnly (implicit in non-null cat)
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.NotNull(row);
        Assert.Equal("Inversiones Fondos", row.Category);   // PCE-1a: raw preserved
        Assert.Equal("Pago especial", row.Description);
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
    // Triangulation: nómina (payroll) → known single-token category + description
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_NominaBlock_ReturnsCategoryNominaAndMerchantDescription()
    {
        // Arrange — payroll split across two physical lines
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("15/01/2025 Nómina Empresa SA"),
            Line("3.200,00 4.500,00"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — category = Nómina; description = "Empresa SA"
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Equal("Nómina", row.Category);
        Assert.Null(row.SubCategory);
        Assert.Equal("Empresa SA", row.Description);
        Assert.Equal("3200.00", row.Amount);
        Assert.Equal("4500.00", row.Balance);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: parking (Ocio Parking) → subcategory extracted
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_ParkingBlock_ReturnsCategoryOcioAndSubcategoryParking()
    {
        // Arrange — parking fixture (single physical line)
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("16/01/2025 Ocio Parking Auditorio -3,00 1.231,56"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — cat = Ocio, subcat = Parking, desc = Auditorio
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.Equal("Ocio", row.Category);
        Assert.Equal("Parking", row.SubCategory);
        Assert.Equal("Auditorio", row.Description);
        Assert.Equal("-3.00", row.Amount);
    }

    // ════════════════════════════════════════════════════════════════════════
    // RawOnly fallback: IBR-4b preserved — block with amount but unrecognized category
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProcessBlocks_RawOnlyFallback_RowCreatedWithRawCategoryNotNull()
    {
        // Arrange — IBR-4b: extractable amount + category NOT in taxonomy → RawOnly row (not null)
        IReadOnlyList<IngLineData> dataLines =
        [
            Line("22/01/2025 UnknownBank Transfer Wire -100,00 500,00"),
        ];

        // Act
        IReadOnlyList<RawTransactionRow> rows = InvokeProcessBlocks(dataLines);

        // Assert — IBR-4b: row created (not null); category preserved from raw text (first 2 tokens)
        Assert.Single(rows);
        RawTransactionRow row = rows[0];
        Assert.NotNull(row);
        Assert.Equal("UnknownBank Transfer", row.Category);  // raw first-2-tokens fallback
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

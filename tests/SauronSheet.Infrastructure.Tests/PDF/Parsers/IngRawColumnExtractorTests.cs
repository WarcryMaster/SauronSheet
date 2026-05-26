namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="IngRawColumnExtractor"/> — geometry-first extraction of
/// category, subcategory, and description from ING Bank PDF block anchor lines.
///
/// Uses the real ING Jan-2025 thresholds fixture:
///   CATEGORÍA=175, SUBCATEGORÍA=275, DESCRIPCIÓN=375, IMPORTE=465
///
/// Column zones:
///   [CategoryStart=175, SubCategoryStart=275)    → Category
///   [SubCategoryStart=275, DescriptionStart=375) → SubCategory
///   [DescriptionStart=375, MonetaryZoneStart=465)→ Description
///   [MonetaryZoneStart=465, ∞)                   → excluded (monetary)
///   [0, CategoryStart=175)                        → ignored (date / F.VALOR)
///
/// Covers:
///   IBR-3a — DAZN: single-word category + subcategory + description
///   IBR-3b — Parking: multi-word category AND multi-word subcategory
///   IBR-3c — Nómina: category present, subcategory absent
///   IBR-3d — Traspaso: multi-word category AND multi-word subcategory (second set)
///   IBR-3e — Geometry insufficient → null/null/null conservative fallback
///   IBR-3f — Monetary zone words excluded from description
///   Continuations → appended to description regardless of X position
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngRawColumnExtractorTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Shared thresholds fixture — Jan-2025 ING real X positions
    // ────────────────────────────────────────────────────────────────────────

    private static IngColumnThresholds Jan2025Thresholds()
    {
        var headerWords = new[]
        {
            new PositionedWord("F.", 10.0),
            new PositionedWord("VALOR", 25.0),
            new PositionedWord("CATEGORÍA", 175.0),
            new PositionedWord("SUBCATEGORÍA", 275.0),
            new PositionedWord("DESCRIPCIÓN", 375.0),
            new PositionedWord("IMPORTE", 465.0),
            new PositionedWord("SALDO", 510.0),
        };
        return IngColumnThresholds.FromHeaderWords(headerWords)!;
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3a: DAZN — single-word per column, three zones fully populated
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_DaznAnchor_ReturnsCategoryOnlineAndDescriptionDazn()
    {
        // Arrange — IBR-3a: "Compras | Online | DAZN" distributed across three zones
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("15/01/2025", 10.0),   // date zone — ignored
            new PositionedWord("Compras", 178.0),      // category zone [175, 275)
            new PositionedWord("Online", 278.0),       // subcategory zone [275, 375)
            new PositionedWord("DAZN", 378.0),         // description zone [375, 465)
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, null, thresholds);

        // Assert — IBR-3a: all three fields populated from geometry
        Assert.Equal("Compras", result.Category);
        Assert.Equal("Online", result.SubCategory);
        Assert.Equal("DAZN", result.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3b: Parking — multi-word category AND multi-word subcategory
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_ParkingMultiWordCategoryAndSubcategory_ReturnsJoinedGroupsCorrectly()
    {
        // Arrange — IBR-3b: "Vehículo y transporte | Parking y garaje | San Isidro"
        // Category words span [175, 275), SubCategory words span [275, 375),
        // Description word in [375, 465)
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("Vehículo",   178.0),  // category zone
            new PositionedWord("y",          200.0),  // category zone
            new PositionedWord("transporte", 228.0),  // category zone
            new PositionedWord("Parking",    278.0),  // subcategory zone
            new PositionedWord("y",          305.0),  // subcategory zone
            new PositionedWord("garaje",     330.0),  // subcategory zone
            new PositionedWord("San",        378.0),  // description zone
            new PositionedWord("Isidro",     405.0),  // description zone
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, null, thresholds);

        // Assert — IBR-3b: multi-word groups joined; description = merchant name
        Assert.Equal("Vehículo y transporte", result.Category);
        Assert.Equal("Parking y garaje", result.SubCategory);
        Assert.Equal("San Isidro", result.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3c: Nómina — category present, no subcategory words
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_NominaAnchor_ReturnsCategoryNominaAndNullSubCategory()
    {
        // Arrange — IBR-3c: "Nominas" in category zone, nothing in subcategory zone,
        // employer name in description zone
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("Nominas",   178.0),  // category zone — no subcategory words
            new PositionedWord("EMPRESA",   378.0),  // description zone
            new PositionedWord("S.A.",      408.0),  // description zone
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, null, thresholds);

        // Assert — IBR-3c: category populated, subcategory null (no words in that zone)
        Assert.Equal("Nominas", result.Category);
        Assert.Null(result.SubCategory);
        Assert.Equal("EMPRESA S.A.", result.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3d: Traspaso — multi-word category + multi-word subcategory (second set)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_TraspasoMultiWordCategoryAndSubcategory_ReturnsCorrectGroups()
    {
        // Arrange — IBR-3d: "Mis cuentas y depósitos | Traspasos propios | Cuenta corriente"
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("Mis",        178.0),  // category zone
            new PositionedWord("cuentas",    200.0),  // category zone
            new PositionedWord("y",          225.0),  // category zone
            new PositionedWord("depósitos",  248.0),  // category zone
            new PositionedWord("Traspasos",  278.0),  // subcategory zone
            new PositionedWord("propios",    315.0),  // subcategory zone
            new PositionedWord("Cuenta",     378.0),  // description zone
            new PositionedWord("corriente",  408.0),  // description zone
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, null, thresholds);

        // Assert — IBR-3d: multi-word category and subcategory both correctly grouped
        Assert.Equal("Mis cuentas y depósitos", result.Category);
        Assert.Equal("Traspasos propios", result.SubCategory);
        Assert.Equal("Cuenta corriente", result.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3e: Geometry insufficient → null/null/null conservative fallback
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_NoWordsInColumnZones_ReturnsNullCategorySubcategoryAndDescription()
    {
        // Arrange — IBR-3e: only date word before CategoryStart=175; no column signal
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("15/01/2025", 10.0),  // pre-category zone — ignored
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, null, thresholds);

        // Assert — IBR-3e: no category signal → conservative null/null/null
        Assert.Null(result.Category);
        Assert.Null(result.SubCategory);
        Assert.Null(result.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-3f: Monetary zone words excluded from description
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_WordsInMonetaryZone_ExcludedFromDescription()
    {
        // Arrange — IBR-3f: amounts appear at X ≥ 465 (MonetaryZoneStart)
        // Only "DAZN" is in the description zone [375, 465); amounts must be excluded
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("Compras",   178.0),  // category zone
            new PositionedWord("Online",    278.0),  // subcategory zone
            new PositionedWord("DAZN",      378.0),  // description zone [375, 465)
            new PositionedWord("-12,99",    468.0),  // monetary zone — EXCLUDED
            new PositionedWord("1.234,56",  492.0),  // monetary zone — EXCLUDED
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, null, thresholds);

        // Assert — IBR-3f: monetary tokens do NOT appear in any output field
        Assert.Equal("Compras", result.Category);
        Assert.Equal("Online", result.SubCategory);
        Assert.Equal("DAZN", result.Description);
        Assert.DoesNotContain("-12,99", result.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("1.234,56", result.Description ?? string.Empty, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Continuations → appended to description (multi-line blocks)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_WithContinuationLines_ContinuationTextAppendedToDescription()
    {
        // Arrange — anchor provides category and subcategory;
        // continuation carries the merchant detail (IBR-3a multi-line variant)
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("Compras",   178.0),  // category zone
            new PositionedWord("Online",    278.0),  // subcategory zone
        };
        var continuations = new[]
        {
            new IngLineData("DAZN Premium", []),  // continuation — text only, no X geometry
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, continuations, thresholds);

        // Assert — continuation text appended to description from anchor
        Assert.Equal("Compras", result.Category);
        Assert.Equal("Online", result.SubCategory);
        Assert.Equal("DAZN Premium", result.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: fallback when geometry insufficient but description zone has words
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Extract_WordsOnlyInDescriptionZone_ReturnsNullCategoryAndPopulatedDescription()
    {
        // Arrange — all column words land in description zone (X ≥ 375);
        // no words in category zone → Category = null, but description preserved
        var thresholds = Jan2025Thresholds();
        var anchorWords = new[]
        {
            new PositionedWord("DAZN",     378.0),  // description zone only
            new PositionedWord("Premium",  400.0),  // description zone only
        };

        // Act
        IngRawColumnResult result = IngRawColumnExtractor.Extract(anchorWords, null, thresholds);

        // Assert — category null (no signal), description populated from description zone
        Assert.Null(result.Category);
        Assert.Null(result.SubCategory);
        Assert.Equal("DAZN Premium", result.Description);
    }
}

namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Unit tests for IngColumnThresholds — position-aware column segmentation
/// for ING Bank single-line PDF rows.
///
/// Real ING January 2025 header X-positions used as the fixture:
///   CATEGORÍA ~175, SUBCATEGORÍA ~275, DESCRIPCIÓN ~375
///
/// Covers:
///   FromHeaderWords happy-path (task 1.1)
///   FromHeaderWords missing words → null (task 1.2)
///   SplitWords three-bucket happy-path — PCE-SLa (task 1.3)
///   SplitWords all-words-in-description fallback → null — PCE-SLb (task 1.4)
///   SplitWords category-only partial result — PCE-SLc (task 1.5)
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngColumnThresholdsTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Shared fixture: Jan-2025 ING header words with real X positions
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a header-words array that mirrors a real ING Jan-2025 PDF header line:
    ///   "F. VALOR   F. OPERACIÓN   CATEGORÍA   SUBCATEGORÍA   DESCRIPCIÓN   IMPORTE   SALDO"
    /// with approximate X positions used to calibrate column thresholds.
    /// </summary>
    private static PositionedWord[] Jan2025HeaderWords() => new[]
    {
        new PositionedWord("F.", 10.0),
        new PositionedWord("VALOR", 25.0),
        new PositionedWord("F.", 75.0),
        new PositionedWord("OPERACIÓN", 90.0),
        new PositionedWord("CATEGORÍA", 175.0),      // CategoryStart
        new PositionedWord("SUBCATEGORÍA", 275.0),   // SubCategoryStart
        new PositionedWord("DESCRIPCIÓN", 375.0),    // DescriptionStart
        new PositionedWord("IMPORTE", 465.0),
        new PositionedWord("SALDO", 510.0),
    };

    private static PositionedWord[] HeaderWordsWithConcepto() => new[]
    {
        new PositionedWord("F.", 10.0),
        new PositionedWord("VALOR", 25.0),
        new PositionedWord("F.", 75.0),
        new PositionedWord("OPERACIÓN", 90.0),
        new PositionedWord("CATEGORÍA", 175.0),
        new PositionedWord("SUBCATEGORÍA", 275.0),
        new PositionedWord("CONCEPTO", 375.0),
        new PositionedWord("IMPORTE", 465.0),
        new PositionedWord("SALDO", 510.0),
    };

    // ════════════════════════════════════════════════════════════════════════
    // Task 1.1: FromHeaderWords happy path — real Jan-2025 X positions
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FromHeaderWords_Jan2025HeaderPositions_ReturnsCorrectThresholds()
    {
        // Arrange
        var headerWords = Jan2025HeaderWords();

        // Act
        var thresholds = IngColumnThresholds.FromHeaderWords(headerWords);

        // Assert — all three thresholds extracted from header fixture
        Assert.NotNull(thresholds);
        Assert.Equal(175.0, thresholds.CategoryStart);
        Assert.Equal(275.0, thresholds.SubCategoryStart);
        Assert.Equal(375.0, thresholds.DescriptionStart);
    }

    [Fact]
    public void FromHeaderWords_HeaderWithConceptoVariant_ReturnsCorrectThresholds()
    {
        // Arrange
        var headerWords = HeaderWordsWithConcepto();

        // Act
        var thresholds = IngColumnThresholds.FromHeaderWords(headerWords);

        // Assert
        Assert.NotNull(thresholds);
        Assert.Equal(175.0, thresholds.CategoryStart);
        Assert.Equal(275.0, thresholds.SubCategoryStart);
        Assert.Equal(375.0, thresholds.DescriptionStart);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Task 1.2: FromHeaderWords missing column headers → null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FromHeaderWords_MissingHeaderWords_ReturnsNull()
    {
        // Arrange — header does NOT contain CATEGORÍA / SUBCATEGORÍA / DESCRIPCIÓN-CONCEPTO
        var headerWords = new[]
        {
            new PositionedWord("F.", 10.0),
            new PositionedWord("VALOR", 25.0),
            new PositionedWord("IMPORTE", 465.0),
            new PositionedWord("SALDO", 510.0),
        };

        // Act
        var thresholds = IngColumnThresholds.FromHeaderWords(headerWords);

        // Assert — no thresholds derivable from this header
        Assert.Null(thresholds);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Task 1.3: SplitWords PCE-SLa — three buckets fully populated
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SplitWords_ThreeBuckets_ReturnsCategorySubCategoryDescription()
    {
        // Arrange — thresholds from real Jan-2025 header
        var thresholds = IngColumnThresholds.FromHeaderWords(Jan2025HeaderWords())!;

        // PCE-SLa fixture: words distributed across all three X zones
        var words = new[]
        {
            // Category zone: CategoryStart(175) ≤ X < SubCategoryStart(275)
            new PositionedWord("Compras", 178.0),

            // SubCategory zone: SubCategoryStart(275) ≤ X < DescriptionStart(375)
            new PositionedWord("Ropa", 278.0),
            new PositionedWord("y", 298.0),
            new PositionedWord("complementos", 315.0),

            // Description zone: X ≥ DescriptionStart(375)
            new PositionedWord("Pago", 378.0),
            new PositionedWord("en", 398.0),
            new PositionedWord("Zara", 415.0),
        };

        // Act
        var result = thresholds.SplitWords(words);

        // Assert — PCE-SLa: all three buckets populated
        Assert.NotNull(result);
        Assert.Equal("Compras", result.Value.Category);
        Assert.Equal("Ropa y complementos", result.Value.SubCategory);
        Assert.Equal("Pago en Zara", result.Value.Description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Task 1.4: SplitWords PCE-SLb — all words in description zone → null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SplitWords_AllWordsInDescriptionZone_ReturnsNull()
    {
        // Arrange — thresholds from real Jan-2025 header
        var thresholds = IngColumnThresholds.FromHeaderWords(Jan2025HeaderWords())!;

        // PCE-SLb fixture: ALL words clustered at X ≥ DescriptionStart(375)
        // — no X signal for category separation
        var words = new[]
        {
            new PositionedWord("Compras", 380.0),
            new PositionedWord("Ropa", 400.0),
            new PositionedWord("y", 418.0),
            new PositionedWord("complementos", 432.0),
        };

        // Act
        var result = thresholds.SplitWords(words);

        // Assert — PCE-SLb: insufficient X signal → conservative fallback
        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Task 1.5: SplitWords PCE-SLc — category populated, subcategory absent
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SplitWords_CategoryOnlyNoSubCategory_ReturnsPartialResult()
    {
        // Arrange — thresholds from real Jan-2025 header
        var thresholds = IngColumnThresholds.FromHeaderWords(Jan2025HeaderWords())!;

        // PCE-SLc fixture: words in category zone + description zone
        // — nothing in subcategory zone (275..374)
        var words = new[]
        {
            // Category zone
            new PositionedWord("Compras", 178.0),

            // Description zone (skipping SubCategory zone entirely)
            new PositionedWord("Pago", 378.0),
            new PositionedWord("en", 398.0),
            new PositionedWord("Zara", 415.0),
        };

        // Act
        var result = thresholds.SplitWords(words);

        // Assert — PCE-SLc: category present, subcategory null (not a fallback)
        Assert.NotNull(result);
        Assert.Equal("Compras", result.Value.Category);
        Assert.Null(result.Value.SubCategory);
        Assert.Equal("Pago en Zara", result.Value.Description);
    }
}

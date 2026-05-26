namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using System.Reflection;
using Xunit;
using SauronSheet.Infrastructure.PDF.Parsers;

/// <summary>
/// Tests for the single-line path of IngBankPdfParser.ParseTextColumns.
///
/// Phase 2 addition: PCE-SLd — verifies that words at or beyond MonetaryZoneStart
/// are excluded from the description column. This test is RED until SplitWords
/// in IngColumnThresholds is updated to respect MonetaryZoneStart (task 2.3/2.4).
///
/// Scenarios covered:
///   PCE-SLa — Jan-2025 positioned words + calibrated thresholds → Category + SubCategory extracted
///   PCE-SLb — All words in description X zone → fallback: Category=null, full text in Description
///   PCE-SLd — Word at MonetaryZoneStart excluded from Description (NEW — task 2.2 RED)
///   Boundary guard — null / whitespace text → all null (kept from previous contract)
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngBankPdfParserSingleLineTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Reflection bootstrap
    // Looks for the THREE-argument overload of ParseTextColumns.
    // ────────────────────────────────────────────────────────────────────────

    private static readonly MethodInfo ParseTextColumnsMethod;

    static IngBankPdfParserSingleLineTests()
    {
        ParseTextColumnsMethod = typeof(IngBankPdfParser)
            .GetMethod(
                "ParseTextColumns",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(PositionedWord[]), typeof(IngColumnThresholds) },
                null)
            ?? throw new InvalidOperationException(
                "ParseTextColumns(string?, PositionedWord[]?, IngColumnThresholds?) not found on " +
                "IngBankPdfParser. The method has not yet been refactored — Phase 4 is required.");
    }

    private static (string? category, string? subCategory, string? description, string? comment)
        InvokeParseTextColumns(string? text, PositionedWord[]? words, IngColumnThresholds? thresholds)
    {
        var parser = new IngBankPdfParser();
        var result = ParseTextColumnsMethod.Invoke(parser, new object?[] { text, words, thresholds });
        return ((string?, string?, string?, string?))result!;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Shared fixture helpers
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Real ING Jan-2025 header thresholds (no MonetaryZoneStart).</summary>
    private static IngColumnThresholds Jan2025Thresholds()
    {
        var headerWords = new[]
        {
            new PositionedWord("CATEGORÍA",   175.0),
            new PositionedWord("SUBCATEGORÍA", 275.0),
            new PositionedWord("CONCEPTO",     375.0),
        };
        return IngColumnThresholds.FromHeaderWords(headerWords)!;
    }

    /// <summary>Jan-2025 thresholds WITH MonetaryZoneStart=465 (IMPORTE column).</summary>
    private static IngColumnThresholds Jan2025ThresholdsWithImporte()
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
    // PCE-SLa: Jan-2025 positioned words → Category + SubCategory extracted
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseTextColumns_Jan2025PositionedWords_ExtractsCategoryAndSubCategory()
    {
        // Arrange — PCE-SLa fixture: real Jan-2025 ING transaction row
        const string text = "Compras Ropa y complementos Pago en Zara Online";

        var words = new[]
        {
            new PositionedWord("Compras",      178.0),   // category zone
            new PositionedWord("Ropa",         278.0),   // subcategory zone
            new PositionedWord("y",            298.0),   // subcategory zone
            new PositionedWord("complementos", 315.0),   // subcategory zone
            new PositionedWord("Pago",         378.0),   // description zone
            new PositionedWord("en",           398.0),   // description zone
            new PositionedWord("Zara",         415.0),   // description zone
            new PositionedWord("Online",       432.0),   // description zone
        };

        var thresholds = Jan2025Thresholds();

        // Act
        var (category, subCategory, description, comment) =
            InvokeParseTextColumns(text, words, thresholds);

        // Assert — PCE-SLa: category and subcategory extracted from X positions
        Assert.Equal("Compras", category);
        Assert.Equal("Ropa y complementos", subCategory);
        Assert.Null(comment);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-SLb: insufficient X signal → conservative fallback
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseTextColumns_InsufficientXSignal_FallsBackToDescription()
    {
        // Arrange — PCE-SLb: ALL words clustered in description X zone (≥375)
        const string text = "Compras Ropa y complementos Pago en Zara Online";

        var words = new[]
        {
            new PositionedWord("Compras",      380.0),
            new PositionedWord("Ropa",         400.0),
            new PositionedWord("y",            418.0),
            new PositionedWord("complementos", 432.0),
            new PositionedWord("Pago",         450.0),
            new PositionedWord("en",           462.0),
            new PositionedWord("Zara",         478.0),
            new PositionedWord("Online",       492.0),
        };

        var thresholds = Jan2025Thresholds();

        // Act
        var (category, subCategory, description, comment) =
            InvokeParseTextColumns(text, words, thresholds);

        // Assert — PCE-SLb: conservative fallback — full text preserved as description
        Assert.Null(category);
        Assert.Null(subCategory);
        Assert.Contains("Compras", description);
        Assert.Null(comment);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-SLd: word at MonetaryZoneStart excluded from description (task 2.2 RED)
    // RED until SplitWords is updated to respect MonetaryZoneStart (task 2.3/2.4).
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseTextColumns_WordAtMonetaryZoneStart_IsExcludedFromDescription()
    {
        // Arrange — word at X=470 (≥ MonetaryZoneStart=465) must NOT appear in Description
        // This is a monetary token that leaked into the text input; SplitWords must exclude it.
        const string text = "Compras DAZN -12,99";

        var words = new[]
        {
            new PositionedWord("Compras", 180.0),   // category zone [175, 275)
            new PositionedWord("DAZN",    380.0),   // description zone [375, 465)
            new PositionedWord("-12,99",  470.0),   // monetary zone ≥465 — must be excluded
        };

        var thresholds = Jan2025ThresholdsWithImporte(); // MonetaryZoneStart = 465

        // Act
        var (category, _, description, _) = InvokeParseTextColumns(text, words, thresholds);

        // Assert — PCE-SLd: "-12,99" at monetary X zone excluded from description
        Assert.Equal("Compras", category);
        Assert.Equal("DAZN",    description);
        Assert.DoesNotContain("-12,99", description ?? string.Empty); // monetary word excluded
    }

    // ════════════════════════════════════════════════════════════════════════
    // Boundary guard: null / whitespace → all null (contract preserved)
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseTextColumns_NullOrWhitespace_ReturnsAllNull(string? input)
    {
        // Act
        var (category, subCategory, description, comment) =
            InvokeParseTextColumns(input, null, null);

        // Assert — boundary guard unchanged
        Assert.Null(category);
        Assert.Null(subCategory);
        Assert.Null(description);
        Assert.Null(comment);
    }
}

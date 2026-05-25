namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using System.Reflection;
using Xunit;
using SauronSheet.Infrastructure.PDF.Parsers;

/// <summary>
/// Documents the single-line path behavior of IngBankPdfParser.ParseTextColumns.
///
/// PCE-1a single-line guard: when all PDF columns are merged into one flat string
/// (single-line format), column boundaries CANNOT be determined without the original
/// column-width data from the PDF renderer. Therefore:
///   - category is always null (no reliable separator)
///   - subCategory is always null (no reliable separator)
///   - description = full remainder text (preserved as-is for BankCategoryResolutionService)
///   - comment is always null
///
/// This is a documented limitation, not a bug. The multi-line path (IngTransactionLineParser)
/// handles position-first extraction; the single-line path cannot.
///
/// See: IngTransactionLineParserTests for the multi-line / position-first coverage (PCE-1a).
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngBankPdfParserSingleLineTests
{
    private static readonly MethodInfo ParseTextColumnsMethod;

    static IngBankPdfParserSingleLineTests()
    {
        ParseTextColumnsMethod = typeof(IngBankPdfParser)
            .GetMethod(
                "ParseTextColumns",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null)
            ?? throw new InvalidOperationException(
                "ParseTextColumns method not found on IngBankPdfParser. " +
                "Method may have been renamed — update this test.");
    }

    private static (string? category, string? subCategory, string? description, string? comment)
        InvokeParseTextColumns(string? text)
    {
        var parser = new IngBankPdfParser();
        var result = ParseTextColumnsMethod.Invoke(parser, new object?[] { text });
        return ((string?, string?, string?, string?)) result!;
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1a single-line guard: category and subCategory are ALWAYS null.
    // Full text preserved as description.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseTextColumns_NonEmptyText_CategoryAndSubCategoryAreNull()
    {
        // Arrange: single-line text that would contain merged category + description
        // (e.g., "Compras Ropa y complementos Pago en Zara Online" all on one line)
        const string mergedText = "Compras Ropa y complementos Pago en Zara Online";

        // Act
        var (category, subCategory, description, comment) =
            InvokeParseTextColumns(mergedText);

        // Assert: PCE-1a single-line guard
        Assert.Null(category);       // cannot separate category without column-width data
        Assert.Null(subCategory);    // cannot separate subcategory without column-width data
        Assert.Equal(mergedText, description);  // full text preserved as description
        Assert.Null(comment);
    }

    // ════════════════════════════════════════════════════════════════════════
    // TRIANGULATE: different input — pure description, no category-like prefix.
    // Verifies the rule holds regardless of text content.
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseTextColumns_DescriptionOnlyText_CategoryAndSubCategoryStillNull()
    {
        // Arrange: text that looks like a pure description (no category prefix)
        const string descriptionText = "Transferencia recibida de JUAN GARCIA LOPEZ";

        // Act
        var (category, subCategory, description, comment) =
            InvokeParseTextColumns(descriptionText);

        // Assert: same contract — no column splitting attempted on single-line
        Assert.Null(category);
        Assert.Null(subCategory);
        Assert.Equal(descriptionText, description);
        Assert.Null(comment);
    }

    // ════════════════════════════════════════════════════════════════════════
    // TRIANGULATE: null / whitespace input → all null (boundary guard).
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseTextColumns_NullOrWhitespace_ReturnsAllNull(string? input)
    {
        // Act
        var (category, subCategory, description, comment) =
            InvokeParseTextColumns(input);

        // Assert
        Assert.Null(category);
        Assert.Null(subCategory);
        Assert.Null(description);
        Assert.Null(comment);
    }
}

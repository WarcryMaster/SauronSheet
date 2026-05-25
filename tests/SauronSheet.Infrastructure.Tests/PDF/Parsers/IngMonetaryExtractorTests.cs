namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="IngMonetaryExtractor"/>.
///
/// Covers:
///   IBR-2a — R→L happy path: last token = balance, penultimate = amount; both normalized.
///   IBR-2b — COMENTARIO contract: result type carries no Comment field;
///             CleanText does not retain monetary tokens.
///   IBR-4a — Fallback conservador: fewer than 2 isolated monetary tokens → null result.
///
/// Strategy: pure unit tests — no I/O, no mocks.
/// Input is plain string text (block FullText); output is nullable <see cref="IngMonetaryResult"/>.
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngMonetaryExtractorTests
{
    // ════════════════════════════════════════════════════════════════════════
    // IBR-2a: R→L happy path — amount and balance extracted and normalized
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractRightToLeft_ValidBlock_ReturnsNormalizedAmountAndBalance()
    {
        // Arrange — IBR-2a: real Jan-2025 DAZN row ending in European-format amounts
        const string blockText = "15/01/2025 Compras Online DAZN -12,99 1.234,56";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert — last token is balance, penultimate is amount; both normalized
        Assert.NotNull(result);
        Assert.Equal("-12.99", result.Value.Amount);
        Assert.Equal("1234.56", result.Value.Balance);
    }

    [Fact]
    public void ExtractRightToLeft_ValidBlock_CleanTextContainsDescriptionNotAmounts()
    {
        // Arrange — triangulation of IBR-2a: verify CleanText is free of monetary tokens
        const string blockText = "15/01/2025 Compras Online DAZN -12,99 1.234,56";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert — CleanText must not contain the raw amount/balance tokens
        Assert.NotNull(result);
        Assert.Contains("DAZN", result.Value.CleanText, StringComparison.Ordinal);
        Assert.DoesNotContain("-12,99", result.Value.CleanText, StringComparison.Ordinal);
        Assert.DoesNotContain("1.234,56", result.Value.CleanText, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractRightToLeft_PositiveAmount_ReturnsCorrectValues()
    {
        // Arrange — triangulation: nómina with positive amount
        const string blockText = "15/01/2025 Nómina Empresa SA 3.200,00 4.500,00";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("3200.00", result.Value.Amount);
        Assert.Equal("4500.00", result.Value.Balance);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-2b: COMENTARIO contract — no Comment field; CleanText is clean
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractRightToLeft_ValidBlock_ResultHasNoCommentField()
    {
        // Arrange — IBR-2b: any valid ING row
        const string blockText = "16/01/2025 Ocio Parking Auditorio -3,00 1.231,56";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert — IBR-2b contract: IngMonetaryResult carries no Comment property;
        // the COMENTARIO column is represented by its absence in the result type.
        Assert.NotNull(result);

        // Verify that the CleanText (what feeds downstream taxonomy extraction)
        // does not contain monetary tokens — they have been fully stripped.
        Assert.DoesNotContain("-3,00", result.Value.CleanText, StringComparison.Ordinal);
        Assert.DoesNotContain("1.231,56", result.Value.CleanText, StringComparison.Ordinal);

        // The description content is preserved in CleanText
        Assert.Contains("Parking", result.Value.CleanText, StringComparison.Ordinal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // IBR-4a: Fallback conservador — fewer than 2 monetary tokens → null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractRightToLeft_NoMonetaryTokens_ReturnsNull()
    {
        // Arrange — IBR-4a: block with no amount-like tokens at all
        const string blockText = "15/01/2025 Compras Online SomeDescription";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert — IBR-4a: fallback conservador — null result, no transaction created
        Assert.Null(result);
    }

    [Fact]
    public void ExtractRightToLeft_OnlyOneMonetaryToken_ReturnsNull()
    {
        // Arrange — IBR-4a triangulation: only one numeric token (not enough for amount+balance)
        const string blockText = "15/01/2025 Compras Online DAZN 1.234,56";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert — single monetary token is insufficient → null
        Assert.Null(result);
    }

    [Fact]
    public void ExtractRightToLeft_EmptyText_ReturnsNull()
    {
        // Arrange — edge case: empty block text
        const string blockText = "";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert — empty input → null (no transaction creatable)
        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Triangulation: multi-line block text (joined continuation lines)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractRightToLeft_MultiLineContinuationBlock_ExtractsFromJoinedText()
    {
        // Arrange — multi-line block where numbers appear in the second physical line
        // (as would happen after IngBlockAssembler joins two lines)
        const string blockText = "15/01/2025 Nómina Empresa SA -12,99 1.234,56";

        // Act
        IngMonetaryResult? result = IngMonetaryExtractor.ExtractRightToLeft(blockText);

        // Assert — extraction works regardless of whether text came from 1 or 2 physical lines
        Assert.NotNull(result);
        Assert.Equal("-12.99", result.Value.Amount);
        Assert.Equal("1234.56", result.Value.Balance);
    }
}

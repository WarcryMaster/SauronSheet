namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using System.Reflection;
using Xunit;
using SauronSheet.Infrastructure.PDF.Parsers;

/// <summary>
/// Tests for PDF parsers' amount normalization with dual-format support.
/// Validates that both European (comma decimal) and Anglo (point decimal) formats are handled correctly.
/// </summary>
public class AmountNormalizationTests
{
    [Theory]
    [InlineData("0.82", "0.82")]                              // Anglo: decimal only
    [InlineData("0,82", "0.82")]                              // European: decimal only
    [InlineData("1,246.74", "1246.74")]                       // Anglo: comma=thousands, point=decimal
    [InlineData("1.246,74", "1246.74")]                       // European: point=thousands, comma=decimal
    [InlineData("-0.82", "-0.82")]                            // Negative: Anglo format
    [InlineData("-0,82", "-0.82")]                            // Negative: European format
    [InlineData("-1,246.74", "-1246.74")]                     // Negative: Anglo with thousands
    [InlineData("-1.246,74", "-1246.74")]                     // Negative: European with thousands
    [InlineData("1000", "1000")]                              // No separator
    [InlineData("1000.50", "1000.50")]                        // No thousands separator, point decimal
    [InlineData("1000,50", "1000.50")]                        // No thousands separator, comma decimal
    public void NormalizeAmount_SupportsBothFormats(string input, string expected)
    {
        // Arrange: Get the NormalizeAmount method from IngBankPdfParser using reflection
        var ingParserType = typeof(IngBankPdfParser);
        var method = ingParserType.GetMethod(
            "NormalizeAmount", 
            BindingFlags.NonPublic | BindingFlags.Static, 
            null, 
            new[] { typeof(string) }, 
            null)
            ?? throw new InvalidOperationException("NormalizeAmount method not found on IngBankPdfParser");

        // Act
        var result = method.Invoke(null, new object?[] { input }) as string;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]                                  // Null input
    [InlineData("", null)]                                    // Empty string
    [InlineData("   ", null)]                                 // Whitespace only
    public void NormalizeAmount_HandlesNullAndEmptyInputs(string? input, string? expected)
    {
        // Arrange: Get the NormalizeAmount method from IngBankPdfParser using reflection
        var ingParserType = typeof(IngBankPdfParser);
        var method = ingParserType.GetMethod(
            "NormalizeAmount", 
            BindingFlags.NonPublic | BindingFlags.Static, 
            null, 
            new[] { typeof(string) }, 
            null)
            ?? throw new InvalidOperationException("NormalizeAmount method not found on IngBankPdfParser");

        // Act
        var result = method.Invoke(null, new object?[] { input }) as string;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0.82", "0.82")]                              // Anglo: decimal only
    [InlineData("0,82", "0.82")]                              // European: decimal only
    [InlineData("1,246.74", "1246.74")]                       // Anglo: comma=thousands, point=decimal
    [InlineData("1.246,74", "1246.74")]                       // European: point=thousands, comma=decimal
    [InlineData("-0.82", "-0.82")]                            // Negative: Anglo format
    [InlineData("-0,82", "-0.82")]                            // Negative: European format
    [InlineData("-1,246.74", "-1246.74")]                     // Negative: Anglo with thousands
    [InlineData("-1.246,74", "-1246.74")]                     // Negative: European with thousands
    public void GenericBankPdfParser_NormalizeAmount_SupportsBothFormats(string input, string expected)
    {
        // Arrange: Get the NormalizeAmount method from GenericBankPdfParser using reflection
        var genericParserType = typeof(GenericBankPdfParser);
        var method = genericParserType.GetMethod(
            "NormalizeAmount", 
            BindingFlags.NonPublic | BindingFlags.Static, 
            null, 
            new[] { typeof(string) }, 
            null)
            ?? throw new InvalidOperationException("NormalizeAmount method not found on GenericBankPdfParser");

        // Act
        var result = method.Invoke(null, new object?[] { input }) as string;

        // Assert
        Assert.Equal(expected, result);
    }
}

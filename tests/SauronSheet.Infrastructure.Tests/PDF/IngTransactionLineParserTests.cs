namespace SauronSheet.Infrastructure.Tests.PDF;

using System.Collections.Generic;
using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Golden regression tests for IngTransactionLineParser position-first extraction.
///
/// These tests guard against regressions introduced by removing KnownCategories /
/// KnownSubCategories from IngBankPdfParser (tasks 2.6 → 2.7).
///
/// PCE-1a contract: any category literal from the PDF MUST be preserved regardless
/// of whether it appeared in the previous closed known-category list.
/// PCE-1b: null category line → category=null.
/// PCE-1c: null subcategory line → subCategory=null.
/// </summary>
[Trait("Category", "Infrastructure")]
public class IngTransactionLineParserTests
{
    // ════════════════════════════════════════════════════════════════════════
    // PCE-1a: former KnownCategories value → preserved via position-first
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractFromMultiLine_FormerKnownCategoryValue_PreservedAsLiteral()
    {
        // "Compras" was in the old KnownCategories list.
        // After removing the list, it should still be extracted via position-first.
        var textLines = new List<string>
        {
            "Compras",
            "Ropa y complementos",
            "Pago en Zara Online"
        };

        var (category, subCategory, description) =
            IngTransactionLineParser.ExtractFromMultiLine(textLines);

        Assert.Equal("Compras", category);                   // PCE-1a: literal preserved
        Assert.Equal("Ropa y complementos", subCategory);
        Assert.Equal("Pago en Zara Online", description);
    }

    // Triangulation PCE-1a: value NOT in old list must now also be captured
    [Fact]
    public void ExtractFromMultiLine_UnknownCategoryValue_CapturedAsLiteral()
    {
        // "Viajes y turismo" was NOT in KnownCategories.
        // Old code would have set category=null and lost the value.
        // With position-first, it must now be captured.
        var textLines = new List<string>
        {
            "Viajes y turismo",
            "Alquiler de coches",
            "Budget Car Spain Madrid"
        };

        var (category, subCategory, description) =
            IngTransactionLineParser.ExtractFromMultiLine(textLines);

        Assert.Equal("Viajes y turismo", category);    // PCE-1a: was lost before — now preserved
        Assert.Equal("Alquiler de coches", subCategory);
        Assert.Equal("Budget Car Spain Madrid", description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1b: no category line (empty textLines) → category=null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractFromMultiLine_EmptyTextLines_AllNull()
    {
        var (category, subCategory, description) =
            IngTransactionLineParser.ExtractFromMultiLine(new List<string>());

        Assert.Null(category);
        Assert.Null(subCategory);
        Assert.Null(description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PCE-1c: no subcategory line → subCategory=null
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractFromMultiLine_OnlyCategoryLine_SubCategoryNull()
    {
        var textLines = new List<string> { "Hogar" };

        var (category, subCategory, description) =
            IngTransactionLineParser.ExtractFromMultiLine(textLines);

        Assert.Equal("Hogar", category);
        Assert.Null(subCategory);   // PCE-1c: no subcategory line
        Assert.Null(description);
    }

    // Category + description, no subcategory (2-line case)
    [Fact]
    public void ExtractFromMultiLine_CategoryAndDescriptionOnly_TwoLines()
    {
        var textLines = new List<string>
        {
            "Otros ingresos",
            "Nómina enero 2024"
        };

        var (category, subCategory, description) =
            IngTransactionLineParser.ExtractFromMultiLine(textLines);

        Assert.Equal("Otros ingresos", category);
        Assert.Equal("Nómina enero 2024", subCategory);   // position-first: line[1] = subcategory
        Assert.Null(description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Multi-line description: lines 3+ joined with space
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractFromMultiLine_MultiLineDescription_JoinedWithSpace()
    {
        var textLines = new List<string>
        {
            "Hogar",
            "Agua",
            "Factura Canal",
            "de Isabel II"
        };

        var (category, subCategory, description) =
            IngTransactionLineParser.ExtractFromMultiLine(textLines);

        Assert.Equal("Hogar", category);
        Assert.Equal("Agua", subCategory);
        Assert.Equal("Factura Canal de Isabel II", description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Whitespace-only lines treated as null (not empty string artefacts)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractFromMultiLine_WhitespaceOnlyLine_TreatedAsNull()
    {
        var textLines = new List<string> { "  ", "Ropa y complementos", "Pago" };

        var (category, subCategory, description) =
            IngTransactionLineParser.ExtractFromMultiLine(textLines);

        Assert.Null(category);                       // whitespace → null
        Assert.Equal("Ropa y complementos", subCategory);
        Assert.Equal("Pago", description);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Sub-category NOT in old KnownSubCategories → preserved (PCE-1a variant)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractFromMultiLine_UnknownSubCategoryValue_CapturedAsLiteral()
    {
        // "Alquiler de coches" was NOT in KnownSubCategories.
        var textLines = new List<string>
        {
            "Viajes y turismo",
            "Alquiler de coches"
        };

        var (category, subCategory, _) =
            IngTransactionLineParser.ExtractFromMultiLine(textLines);

        Assert.Equal("Viajes y turismo", category);
        Assert.Equal("Alquiler de coches", subCategory);   // was lost before — now captured
    }
}

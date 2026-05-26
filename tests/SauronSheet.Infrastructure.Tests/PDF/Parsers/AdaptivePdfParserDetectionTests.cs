namespace SauronSheet.Infrastructure.Tests.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Infrastructure.PDF.Parsers;
using Xunit;

/// <summary>
/// Exercises the real <see cref="AdaptivePdfParser"/> detection and dispatch path.
/// The test PDFs are intentionally minimal but valid enough for PdfPig to read the
/// first-page words and trigger the ING header scan.
/// </summary>
[Trait("Category", "Infrastructure")]
public class AdaptivePdfParserDetectionTests
{
    [Fact]
    public async Task ParseAsync_IngHeaderDetected_DispatchesToIngParser()
    {
        var ingParser = new IngBankPdfParser();
        var genericParser = new GenericBankPdfParser();
        var parser = new AdaptivePdfParser(ingParser, genericParser);

        using MemoryStream pdfStream = CreatePdfWithLines(
        [
            "F. VALOR CATEGORÍA SUBCATEGORÍA CONCEPTO IMPORTE SALDO",
            "15/01/2025 Compras Online DAZN -12,99 1.234,56",
        ]);

        List<RawTransactionRow> rows = await parser.ParseAsync(pdfStream);

        // Primary assertion: ING parser was dispatched and produced a row with the correct
        // monetary data and date (IBR-5a / dispatch contract).
        Assert.Single(rows);
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("-12.99", rows[0].Amount);
        Assert.Equal("1234.56", rows[0].Balance);

        // Description: the synthetic PDF places all words at base X=72 (one Tj per line),
        // so geometry-based column zones may not align with real ING column positions.
        // Phase 2 does NOT assert exact category/subcategory values for synthetic PDFs.
        // Real column extraction is covered by IngBankPdfParserBlockTests with positioned words.
        Assert.NotNull(rows[0].Description);
        Assert.DoesNotContain("-12,99", rows[0].Description, StringComparison.Ordinal);
        Assert.DoesNotContain("15/01/2025", rows[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_IngHeaderAbsent_DispatchesToGenericParser()
    {
        var ingParser = new IngBankPdfParser();
        var genericParser = new GenericBankPdfParser();
        var parser = new AdaptivePdfParser(ingParser, genericParser);

        using MemoryStream pdfStream = CreatePdfWithLines(
        [
            "MOVIMIENTOS",
            "15/01/2025 Compra Zara -50.00 EUR",
        ]);

        List<RawTransactionRow> rows = await parser.ParseAsync(pdfStream);

        Assert.Single(rows);
        Assert.Equal("15/01/2025", rows[0].Date);
        Assert.Equal("Compra Zara", rows[0].Description);
        Assert.Equal("-50.00", rows[0].Amount);
        Assert.Null(rows[0].Balance);
        Assert.Equal("EUR", rows[0].Currency);
    }

    private static MemoryStream CreatePdfWithLines(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        Encoding pdfEncoding = Encoding.Latin1;
        StringBuilder contentBuilder = new();
        double currentY = 780;
        foreach (string line in lines)
        {
            string escapedLine = EscapePdfText(line);
            contentBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                "BT /F1 12 Tf 72 {0:0} Td ({1}) Tj ET\n",
                currentY,
                escapedLine);
            currentY -= 18;
        }

        string content = contentBuilder.ToString();
        byte[] contentBytes = pdfEncoding.GetBytes(content);

        StringBuilder pdfBuilder = new();
        var offsets = new List<int>();

        void AppendObject(string value)
        {
            offsets.Add(pdfEncoding.GetByteCount(pdfBuilder.ToString()));
            pdfBuilder.Append(value);
        }

        pdfBuilder.Append("%PDF-1.4\n");
        AppendObject("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        AppendObject("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        AppendObject("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");
        AppendObject("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        AppendObject($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{content}endstream\nendobj\n");

        int xrefOffset = pdfEncoding.GetByteCount(pdfBuilder.ToString());
        pdfBuilder.AppendFormat(CultureInfo.InvariantCulture, "xref\n0 {0}\n", offsets.Count + 1);
        pdfBuilder.Append("0000000000 65535 f \n");
        foreach (int offset in offsets)
        {
            pdfBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:0000000000} 00000 n \n", offset);
        }

        pdfBuilder.AppendFormat(
            CultureInfo.InvariantCulture,
            "trailer\n<< /Size {0} /Root 1 0 R >>\nstartxref\n{1}\n%%EOF",
            offsets.Count + 1,
            xrefOffset);

        return new MemoryStream(pdfEncoding.GetBytes(pdfBuilder.ToString()));
    }

    private static string EscapePdfText(string value)
    {
        StringBuilder escaped = new();
        foreach (char character in value)
        {
            if (character is '\\' or '(' or ')')
            {
                escaped.Append('\\');
                escaped.Append(character);
                continue;
            }

            if (character <= 0x7F)
            {
                escaped.Append(character);
                continue;
            }

            escaped.Append('\\');
            escaped.Append(Convert.ToString(character, 8).PadLeft(3, '0'));
        }

        return escaped.ToString();
    }
}

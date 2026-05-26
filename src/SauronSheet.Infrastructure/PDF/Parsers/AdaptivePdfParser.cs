namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using UglyToad.PdfPig;

/// <summary>
/// Adaptive PDF parser that automatically detects the bank format and delegates
/// to the appropriate specialized parser.
///
/// Detection strategy (IBR-5):
///   1. Open the first page of the PDF.
///   2. Extract text lines via <see cref="IngBankPdfParser.HasIngHeaderInLines"/>.
///   3. If the ING header tokens ("F. VALOR" + "CATEGORÍA") are present →
///      use <see cref="IngBankPdfParser"/>.
///   4. Otherwise fall back to <see cref="GenericBankPdfParser"/>.
///
/// This O(1-page) header-scan replaces the previous full-parse detection strategy.
/// </summary>
public class AdaptivePdfParser : IPdfParser
{
    private readonly IngBankPdfParser _ingParser;
    private readonly GenericBankPdfParser _genericParser;

    public AdaptivePdfParser(IngBankPdfParser ingParser, GenericBankPdfParser genericParser)
    {
        _ingParser = ingParser ?? throw new ArgumentNullException(nameof(ingParser));
        _genericParser = genericParser ?? throw new ArgumentNullException(nameof(genericParser));
    }

    /// <summary>
    /// Detects the bank format and dispatches to the correct parser.
    /// </summary>
    public async Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        try
        {
            byte[] pdfBytes = await ReadPdfBytesAsync(pdfStream).ConfigureAwait(false);

            bool isIngFormat = HasIngHeader(pdfBytes);

            if (isIngFormat)
            {
                SentrySdk.Logger?.LogInfo(
                    "AdaptivePdfParser: ING header detected — using IngBankPdfParser");
                return await _ingParser.ParseAsync(pdfBytes).ConfigureAwait(false);
            }

            SentrySdk.Logger?.LogInfo(
                "AdaptivePdfParser: ING header absent — using GenericBankPdfParser");
            return await _genericParser.ParseAsync(pdfBytes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SentrySdk.Logger?.LogError("AdaptivePdfParser: parse error — {0}", ex.Message);
            throw;
        }
    }

    private static async Task<byte[]> ReadPdfBytesAsync(Stream pdfStream)
    {
        using MemoryStream memoryStream = new();
        await pdfStream.CopyToAsync(memoryStream).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    // ────────────────────────────────────────────────────────────────────────
    // IBR-5: O(1-page) header-based ING detection
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans the <em>first page only</em> of the PDF for the ING Bank header tokens
    /// (<c>"F. VALOR"</c> and <c>"CATEGORÍA"</c>).
    ///
    /// O(1 pages) — reads exactly one page regardless of document length.
    /// Replaces the previous full-parse row-count strategy.
    /// </summary>
    /// <param name="pdfBytes">
    /// The fully materialized PDF bytes shared with the selected downstream parser.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the ING Bank header is found; otherwise <see langword="false"/>.
    /// </returns>
    internal static bool HasIngHeader(byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        try
        {
            using PdfDocument document = PdfDocument.Open(pdfBytes);

            foreach (var page in document.GetPages())
            {
                // Reconstruct line texts from the first page only (O(1) pages)
                var words = page.GetWords().ToList();

                if (words.Count == 0)
                    break;

                // Group words by Y coordinate (same logic as IngBankPdfParser.ReconstructLinesFromWords)
                const double yTolerance = 3.0;
                var lines = new List<IngLineData>();
                var lineGroups = new List<List<UglyToad.PdfPig.Content.Word>>();
                var sortedByY = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();

                List<UglyToad.PdfPig.Content.Word>? currentGroup = null;
                double currentY = double.MaxValue;

                foreach (var word in sortedByY)
                {
                    double wordY = word.BoundingBox.Bottom;
                    if (currentGroup == null || Math.Abs(wordY - currentY) > yTolerance)
                    {
                        currentGroup = [word];
                        lineGroups.Add(currentGroup);
                        currentY = wordY;
                    }
                    else
                    {
                        currentGroup.Add(word);
                    }
                }

                foreach (var group in lineGroups)
                {
                    string lineText = string.Join(
                        " ",
                        group.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));

                    lines.Add(new IngLineData(lineText, []));
                }

                // Delegate to the pure header detection function on IngBankPdfParser
                return IngBankPdfParser.HasIngHeaderInLines(lines);
            }
        }
        catch (Exception ex)
        {
            // Unreadable PDF → not ING format
            SentrySdk.Logger?.LogWarning(
                "AdaptivePdfParser.HasIngHeader: PDF read error — {0}", ex.Message);
        }

        return false;
    }
}

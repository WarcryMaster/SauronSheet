namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class IngBankPdfParser : IPdfParser
{
    private static readonly Regex MonetaryTokenPattern = new(
        @"^-?\d[\d.,]*[,\.]\d{2}$",
        RegexOptions.Compiled);

    public async Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);

        using MemoryStream memoryStream = new();
        await pdfStream.CopyToAsync(memoryStream).ConfigureAwait(false);
        return await ParseAsync(memoryStream.ToArray()).ConfigureAwait(false);
    }

    internal Task<List<RawTransactionRow>> ParseAsync(byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        var rows = new List<RawTransactionRow>();

        SentrySdk.Logger?.LogInfo("IngBankPdfParser: attempting to parse ING Bank PDF format");
        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var allLines = new List<IngLineData>();
            var pageCount = document.NumberOfPages;

            SentrySdk.AddBreadcrumb(
                $"PDF opened: {pageCount} pages",
                "pdf.parse",
                data: new Dictionary<string, string> { ["pages"] = pageCount.ToString() });

            // 1. Extract all lines with positions from every page
            var pageNumber = 0;
            foreach (var page in document.GetPages())
            {
                pageNumber++;
                var words = page.GetWords().ToList();
                var reconstructedLines = ReconstructLinesFromWords(words);
                IReadOnlyList<IngLineData> sanitizedLines = pageNumber == 1
                    ? reconstructedLines
                    : StripLeadingRepeatedPageHeaderSection(reconstructedLines);

                allLines.AddRange(sanitizedLines);

                SentrySdk.AddBreadcrumb(
                    $"Page {pageNumber}: {words.Count} words → {reconstructedLines.Count} lines ({sanitizedLines.Count} kept)",
                    "pdf.parse",
                    data: new Dictionary<string, string>
                    {
                        ["page"] = pageNumber.ToString(),
                        ["words"] = words.Count.ToString(),
                        ["lines"] = reconstructedLines.Count.ToString(),
                        ["keptLines"] = sanitizedLines.Count.ToString()
                    });
            }

            SentrySdk.AddBreadcrumb(
                $"Total lines extracted: {allLines.Count}",
                "pdf.parse",
                data: new Dictionary<string, string> { ["totalLines"] = allLines.Count.ToString() });

            // 2. Locate the ING header line to find the start of the data section
            var headerLineIndex = -1;
            for (var i = 0; i < allLines.Count; i++)
            {
                if (HasIngHeaderInLines([allLines[i]]))
                {
                    headerLineIndex = i;
                    SentrySdk.Logger?.LogDebug("IngBankPdfParser: detected ING header at line {0}", i);
                    SentrySdk.AddBreadcrumb("Header section detected", "pdf.parse");
                    break;
                }
            }

            if (headerLineIndex < 0)
            {
                SentrySdk.Logger?.LogDebug(
                    "IngBankPdfParser: ING header not found — returning empty result");
                return Task.FromResult(rows);
            }

            // 3. Wire the block-first pipeline for all data lines after the first header.
            // Repeated per-page headers are stripped during per-page extraction so they
            // cannot contaminate the previous open block as continuation text.
            IReadOnlyList<IngLineData> dataLines = allLines
                .Skip(headerLineIndex + 1)
                .ToArray();

            rows.AddRange(ProcessBlocks(dataLines));

            SentrySdk.Logger?.LogDebug(
                "IngBankPdfParser complete: {0} rows parsed", rows.Count);
        }
        catch (Exception ex) when (ex.Message.Contains("password") ||
                                    ex.Message.Contains("encrypted"))
        {
            SentrySdk.Logger?.LogError("IngBankPdfParser: PDF is password-protected or encrypted");
            throw new InvalidOperationException(
                "El PDF no se puede leer. Puede estar protegido con contraseña o corrupto.", ex);
        }
        catch (Exception ex)
        {
            SentrySdk.Logger?.LogError("IngBankPdfParser: unexpected error — {0}", ex.Message);
            throw new InvalidOperationException(
                $"Error inesperado al parsear el PDF: {ex.Message}", ex);
        }

        return Task.FromResult(rows);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Block-first pipeline (IBR-1 → IBR-2 → IBR-3 → IBR-4)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Processes a flat list of physical data lines through the block-first pipeline:
    /// <list type="number">
    ///   <item><see cref="IngBlockAssembler.Assemble"/> — groups lines into logical blocks.</item>
    ///   <item><see cref="IngMonetaryExtractor.ExtractRightToLeft"/> — R→L monetary extraction.</item>
    ///   <item><see cref="IngControlledTaxonomy.ExtractLeftToRight"/> — L→R taxonomy extraction
    ///         for category, subcategory and clean description (IBR-3).</item>
    ///   <item>Emits <see cref="RawTransactionRow"/> for each block with two isolated monetary tokens.</item>
    /// </list>
    /// When the taxonomy does not recognise the category prefix, the raw leading tokens are
    /// preserved as the category literal (<see cref="IngTaxonomyResult.IsRawOnly"/> = true; IBR-3b, PCE-1a).
    /// </summary>
    /// <param name="dataLines">
    /// Physical PDF lines from the data section (all lines <em>after</em> the ING header line).
    /// </param>
    /// <returns>
    /// Ordered list of successfully parsed rows. Blocks without two monetary tokens are silently
    /// skipped (IBR-4a conservador fallback).
    /// </returns>
    internal static IReadOnlyList<RawTransactionRow> ProcessBlocks(
        IReadOnlyList<IngLineData> dataLines)
    {
        ArgumentNullException.ThrowIfNull(dataLines);

        var rows = new List<RawTransactionRow>();
        int rowNumber = 1;

        IReadOnlyList<IngBlock> blocks = IngBlockAssembler.Assemble(dataLines);

        foreach (IngBlock block in blocks)
        {
            // IBR-4a: no two monetary tokens → skip (fallback conservador)
            IngMonetaryResult? monetary =
                IngMonetaryExtractor.ExtractRightToLeft(block.FullText);

            if (monetary is null)
            {
                SentrySdk.AddBreadcrumb(
                    $"Block {block.Date} skipped: fewer than 2 monetary tokens (IBR-4a)",
                    "pdf.row",
                    level: BreadcrumbLevel.Warning,
                    data: new Dictionary<string, string> { ["date"] = block.Date });
                continue;
            }

            // Strip the date prefix from CleanText to isolate the taxonomy/description text.
            // CleanText = everything before the amount token.
            // When buffer lines are prepended before the anchor (IBR-1d/IBR-1e), the date may
            // not be at position 0 of cleanText — search for it rather than slicing blindly.
            string cleanText = monetary.Value.CleanText;
            string? taxonomyInput = ExtractTaxonomyInput(cleanText, block.Date);

            // IBR-3: L→R taxonomy extraction — category, subcategory, description.
            // When no prefix matches (IsRawOnly=true), raw leading tokens are preserved as
            // the category literal (IBR-3b, PCE-1a). Taxonomy never returns null for a
            // non-empty input — it always produces at least a RawOnly result.
            IngTaxonomyResult taxonomy =
                IngControlledTaxonomy.ExtractLeftToRight(taxonomyInput);

            var row = new RawTransactionRow(
                rowNumber++,
                block.Date,
                taxonomy.Category,
                taxonomy.SubCategory,
                taxonomy.Description,
                null,                        // Comment — IBR-2b: always null for ING
                monetary.Value.Amount,
                monetary.Value.Balance);

            rows.Add(row);

            SentrySdk.AddBreadcrumb(
                $"Row {rowNumber - 1} parsed: {block.Date} | {taxonomy.Description} | {monetary.Value.Amount} (rawOnly={taxonomy.IsRawOnly})",
                "pdf.row",
                data: new Dictionary<string, string>
                {
                    ["row"] = (rowNumber - 1).ToString(CultureInfo.InvariantCulture),
                    ["date"] = block.Date,
                    ["category"] = taxonomy.Category ?? string.Empty,
                    ["subCategory"] = taxonomy.SubCategory ?? string.Empty,
                    ["description"] = taxonomy.Description ?? string.Empty,
                    ["amount"] = monetary.Value.Amount,
                    ["rawOnly"] = taxonomy.IsRawOnly.ToString()
                });
        }

        SentrySdk.Logger?.LogDebug(
            "IngBankPdfParser.ProcessBlocks: {0} blocks assembled, {1} rows emitted",
            blocks.Count,
            rows.Count);

        return rows;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Header detection (IBR-5)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when any line in <paramref name="lines"/> contains
    /// both <c>"F. VALOR"</c> and <c>"CATEGORÍA"</c> — the two tokens that uniquely identify
    /// an ING Bank statement header (IBR-5a/5b).
    ///
    /// This method is used by <see cref="AdaptivePdfParser"/> for O(1-page) format detection
    /// without requiring a full parse attempt.
    /// </summary>
    internal static bool HasIngHeaderInLines(IReadOnlyList<IngLineData> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        foreach (IngLineData line in lines)
        {
            string trimmed = line.Text.Trim();
            if (trimmed.Contains("F. VALOR", StringComparison.Ordinal) &&
                trimmed.Contains("CATEGORÍA", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes the repeated ING page-header section from a page-local line list.
    ///
    /// Repeated headers can span multiple non-date lines around the ING column-header
    /// row. The parser strips the whole leading section and stops at the first line
    /// that looks like transaction data so continuation rows can still flow into block
    /// assembly.
    /// </summary>
    internal static IReadOnlyList<IngLineData> StripLeadingRepeatedPageHeaderSection(
        IReadOnlyList<IngLineData> pageLines)
    {
        ArgumentNullException.ThrowIfNull(pageLines);

        var headerLineIndex = -1;
        for (var i = 0; i < pageLines.Count; i++)
        {
            if (HasIngHeaderInLines([pageLines[i]]))
            {
                headerLineIndex = i;
                break;
            }
        }

        if (headerLineIndex < 0)
            return pageLines.ToArray();

        int searchStartIndex = headerLineIndex + 1;
        int firstTransactionDateIndex = -1;
        int firstMonetaryLineIndex = -1;

        for (int index = searchStartIndex; index < pageLines.Count; index++)
        {
            string trimmed = pageLines[index].Text.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (StartsWithTransactionDate(trimmed))
            {
                firstTransactionDateIndex = index;
                break;
            }

            if (firstMonetaryLineIndex < 0 && ContainsMonetaryToken(trimmed))
                firstMonetaryLineIndex = index;
        }

        if (firstTransactionDateIndex >= 0)
        {
            int contentStartIndex = FindLeadingContentStartIndex(
                pageLines,
                firstTransactionDateIndex,
                searchStartIndex);

            return pageLines.Skip(contentStartIndex).ToArray();
        }

        if (firstMonetaryLineIndex >= 0)
        {
            int contentStartIndex = FindLeadingContentStartIndex(
                pageLines,
                firstMonetaryLineIndex,
                searchStartIndex);

            return pageLines.Skip(contentStartIndex).ToArray();
        }

        int firstContinuationContentIndex = FindFirstContinuationContentIndex(pageLines, searchStartIndex);
        return firstContinuationContentIndex >= 0
            ? pageLines.Skip(firstContinuationContentIndex).ToArray()
            : [];
    }

    private static int FindLeadingContentStartIndex(
        IReadOnlyList<IngLineData> pageLines,
        int anchorIndex,
        int minimumIndex)
    {
        int contentStartIndex = anchorIndex;

        for (int index = FindPreviousNonEmptyLineIndex(pageLines, anchorIndex - 1, minimumIndex);
             index >= minimumIndex;
             index = FindPreviousNonEmptyLineIndex(pageLines, index - 1, minimumIndex))
        {
            string trimmed = pageLines[index].Text.Trim();
            if (IsRepeatedPageHeaderLine(trimmed) || StartsWithTransactionDate(trimmed))
                break;

            contentStartIndex = index;
        }

        return contentStartIndex;
    }

    private static int FindFirstContinuationContentIndex(
        IReadOnlyList<IngLineData> pageLines,
        int startIndex)
    {
        for (int index = startIndex; index < pageLines.Count; index++)
        {
            string trimmed = pageLines[index].Text.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || IsRepeatedPageHeaderLine(trimmed))
                continue;

            return index;
        }

        return -1;
    }

    private static int FindPreviousNonEmptyLineIndex(
        IReadOnlyList<IngLineData> pageLines,
        int startIndex,
        int minimumIndex)
    {
        for (int index = startIndex; index >= minimumIndex; index--)
        {
            if (string.IsNullOrWhiteSpace(pageLines[index].Text))
                continue;

            return index;
        }

        return -1;
    }

    private static bool ContainsMonetaryToken(string text)
    {
        string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (MonetaryTokenPattern.IsMatch(token))
                return true;
        }

        return false;
    }

    private static bool IsRepeatedPageHeaderLine(string text)
    {
        return text.StartsWith("Página ", StringComparison.Ordinal) ||
               text.StartsWith("Saldo anterior", StringComparison.Ordinal) ||
               text.Contains("Cuenta NARANJA", StringComparison.Ordinal) ||
               text.Contains("Resumen de movimientos", StringComparison.Ordinal);
    }

    private static bool StartsWithTransactionDate(string text)
    {
        return text.Length >= 10 &&
               char.IsDigit(text[0]) &&
               char.IsDigit(text[1]) &&
               text[2] == '/' &&
               char.IsDigit(text[3]) &&
               char.IsDigit(text[4]) &&
               text[5] == '/' &&
               char.IsDigit(text[6]) &&
               char.IsDigit(text[7]) &&
               char.IsDigit(text[8]) &&
               char.IsDigit(text[9]);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Line reconstruction (unchanged from original — supports single-line tests)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reconstructs lines from the positioned words on a PDF page.
    /// Words with the same Y coordinate (within a tolerance) belong to the same line.
    /// Returns <see cref="IngLineData"/> carrying both text and X-positioned words.
    /// </summary>
    private List<IngLineData> ReconstructLinesFromWords(List<Word> words)
    {
        if (!words.Any())
            return new List<IngLineData>();

        const double yTolerance = 3.0;

        var lineGroups = new List<List<Word>>();
        var sortedByY = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();

        List<Word>? currentLine = null;
        double currentY = double.MaxValue;

        foreach (var word in sortedByY)
        {
            var wordY = word.BoundingBox.Bottom;

            if (currentLine == null || Math.Abs(wordY - currentY) > yTolerance)
            {
                currentLine = new List<Word> { word };
                lineGroups.Add(currentLine);
                currentY = wordY;
            }
            else
            {
                currentLine.Add(word);
            }
        }

        var result = new List<IngLineData>();
        foreach (var group in lineGroups)
        {
            var orderedWords = group.OrderBy(w => w.BoundingBox.Left).ToList();
            var lineText = string.Join(" ", orderedWords.Select(w => w.Text));
            var positionedWords = orderedWords
                .Select(w => new PositionedWord(w.Text, w.BoundingBox.Left))
                .ToArray();
            result.Add(new IngLineData(lineText, positionedWords));
        }

        return result;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Taxonomy input extraction
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the taxonomy input text by stripping the date token from
    /// <paramref name="cleanText"/> at whatever position it occurs.
    ///
    /// The standard case is that the date is at position 0 (no prepended buffer).
    /// When the anchor-line assembler (IBR-1d/IBR-1e) prepends buffer lines,
    /// the date token appears after the buffer content and must be found by
    /// <see cref="string.IndexOf"/> rather than a fixed offset.
    ///
    /// Joining the <c>beforeDate</c> and <c>afterDate</c> fragments preserves
    /// the buffer description so downstream taxonomy can classify the full
    /// transaction context.
    /// </summary>
    private static string? ExtractTaxonomyInput(string cleanText, string date)
    {
        if (string.IsNullOrWhiteSpace(cleanText))
            return null;

        int dateIdx = cleanText.IndexOf(date, StringComparison.Ordinal);
        if (dateIdx < 0)
            return cleanText.Trim();

        string afterDate = cleanText[(dateIdx + date.Length)..].Trim();

        if (dateIdx == 0)
            return string.IsNullOrWhiteSpace(afterDate) ? null : afterDate;

        // Buffer lines were prepended before the anchor date (IBR-1d/IBR-1e)
        string beforeDate = cleanText[..dateIdx].Trim();

        if (string.IsNullOrWhiteSpace(afterDate))
            return string.IsNullOrWhiteSpace(beforeDate) ? null : beforeDate;

        return string.IsNullOrWhiteSpace(beforeDate) ? afterDate : $"{beforeDate} {afterDate}";
    }

    // ────────────────────────────────────────────────────────────────────────
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts category, subcategory, description, and comment from the text of a
    /// single-line ING row when X-position geometry is available.
    ///
    /// When <paramref name="words"/> and <paramref name="thresholds"/> are both provided,
    /// the method segments columns by X coordinate (PCE-SLa/PCE-SLc).
    /// When the X signal is insufficient (PCE-SLb), the conservative fallback applies:
    /// all text is returned as description and category/subcategory are null.
    /// </summary>
    private (string? category, string? subCategory, string? description, string? comment)
        ParseTextColumns(string? text, PositionedWord[]? words = null, IngColumnThresholds? thresholds = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null, null, null);

        if (words is { Length: > 0 } && thresholds is not null)
        {
            var split = thresholds.SplitWords(words);
            if (split is not null)
            {
                return (split.Value.Category, split.Value.SubCategory, split.Value.Description, null);
            }

            SentrySdk.AddBreadcrumb(
                "ParseTextColumns: X-signal insufficient — fallback to full-text description",
                "pdf.parse",
                data: new Dictionary<string, string>
                {
                    ["text"] = text.Length > 100 ? text[..100] : text
                });
        }

        var description = text.Trim();
        return (null, null, string.IsNullOrEmpty(description) ? null : description, null);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Amount normalization (kept — tested via reflection by AmountNormalizationTests)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Normalizes a raw monetary string to dot-decimal, no-thousands format.
    /// Supports both European (<c>1.234,56</c>) and Anglo (<c>1,234.56</c>) formats.
    /// </summary>
    private static string? NormalizeAmount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount))
            return null;

        amount = amount.Trim();

        if (!amount.Contains(',') && !amount.Contains('.'))
            return amount;

        if (amount.Contains(',') && amount.Contains('.'))
        {
            var lastCommaIndex = amount.LastIndexOf(',');
            var lastDotIndex = amount.LastIndexOf('.');

            string normalized;
            if (lastCommaIndex > lastDotIndex)
            {
                normalized = amount
                    .Replace(".", string.Empty)
                    .Replace(",", ".");
            }
            else
            {
                normalized = amount.Replace(",", string.Empty);
            }

            return normalized;
        }

        if (amount.Contains(',') && !amount.Contains('.'))
            return amount.Replace(",", ".");

        return amount;
    }

}

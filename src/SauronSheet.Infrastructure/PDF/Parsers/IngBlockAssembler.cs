namespace SauronSheet.Infrastructure.PDF.Parsers;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Pure static helper that assembles <see cref="IngBlock"/> logical transaction
/// blocks from a flat sequence of physical PDF lines.
///
/// <b>Assembly rule (IBR-1):</b>
/// <list type="bullet">
///   <item>A new block starts when the line text begins with a date pattern
///         <c>dd/mm/yyyy</c> (the ING value-date column).</item>
///   <item>A date line is a <em>strong anchor</em> when it also carries an
///         isolatable monetary pair (via <see cref="IngMonetaryExtractor"/>).
///         A <em>strong anchor</em> marks the current block as <em>complete</em>.</item>
///   <item>Non-date lines after a <em>complete</em> block accumulate in an
///         <em>ambiguous buffer</em>. When a new strong anchor arrives, the
///         buffer is prepended to the new block (IBR-1d/IBR-1e). At EOF the
///         buffer is re-appended to the current block.</item>
///   <item>Non-date lines after an <em>incomplete</em> block are appended
///         directly to the block (backward behaviour — IBR-1b/IBR-1f).</item>
///   <item>Lines before the first date-line are silently ignored.</item>
/// </list>
///
/// This class is <c>internal</c> so it can be tested from
/// <c>SauronSheet.Infrastructure.Tests</c> without leaking to outer layers.
/// </summary>
internal static class IngBlockAssembler
{
    // dd/mm/yyyy at the start of the trimmed line text
    private static readonly Regex DatePrefixPattern = new(
        @"^\d{2}/\d{2}/\d{4}",
        RegexOptions.Compiled);

    // Exact date token used by the positioned-word path.
    private static readonly Regex ExactDatePattern = new(
        @"^\d{2}/\d{2}/\d{4}$",
        RegexOptions.Compiled);

    // ING Jan-2025 layouts place the date/value columns well to the left of the
    // category column (~175pt). Keep the first-column check conservative.
    private const double FirstColumnMaxLeft = 120.0;

    /// <summary>
    /// Assembles logical transaction blocks from an ordered list of physical
    /// PDF lines.
    /// </summary>
    /// <param name="lines">
    /// Ordered physical lines extracted from the PDF (all lines in the data
    /// section, after the header has been stripped). Must not be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An ordered, read-only list of <see cref="IngBlock"/> values — one per
    /// logical ING transaction. Returns an empty list when <paramref name="lines"/>
    /// contains no date-starting line.
    /// </returns>
    internal static IReadOnlyList<IngBlock> Assemble(IReadOnlyList<IngLineData> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        List<IngBlock> blocks = [];

        // Accumulator for the current open block
        string? currentDate = null;
        List<IngLineData> currentLines = [];

        // State for the new anchor-aware assembly (IBR-1d/IBR-1e)
        bool isComplete = false;
        List<IngLineData> ambiguousBuffer = [];

        foreach (IngLineData lineData in lines)
        {
            if (TryGetBlockStartDate(lineData, out string? blockDate))
            {
                // Resolve completeness on the same line (avoids a second TryGetBlockStartDate call).
                bool strongAnchor = IsStrongAnchor(lineData);

                if (strongAnchor)
                {
                    // Strong anchor: flush current block, prepend buffer to new block (IBR-1d/IBR-1e)
                    FlushBlock(blocks, currentDate, currentLines);

                    currentDate = blockDate;
                    currentLines = [.. ambiguousBuffer, lineData];
                    ambiguousBuffer = [];
                    isComplete = true;
                }
                else
                {
                    // Incomplete anchor: buffer belongs to current block — re-append then flush
                    currentLines.AddRange(ambiguousBuffer);
                    ambiguousBuffer = [];
                    FlushBlock(blocks, currentDate, currentLines);

                    currentDate = blockDate;
                    currentLines = [lineData];
                    isComplete = false;
                }
            }
            else if (currentDate is not null)
            {
                if (isComplete)
                    ambiguousBuffer.Add(lineData);  // hold until next anchor or EOF
                else
                    currentLines.Add(lineData);     // backward — IBR-1b/IBR-1f
            }
            // Lines before the first date are silently ignored
        }

        // EOF: re-append any pending ambiguous buffer into the current block
        if (ambiguousBuffer.Count > 0)
            currentLines.AddRange(ambiguousBuffer);

        // Flush the final open block
        FlushBlock(blocks, currentDate, currentLines);

        return blocks;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="lineData"/> is a
    /// <em>strong anchor</em>: the line starts with a date in the first column
    /// AND carries an isolatable monetary pair (importe + saldo).
    ///
    /// A strong anchor marks the opened block as <em>complete</em>, enabling
    /// the ambiguous-buffer behaviour for subsequent non-date lines (IBR-1d/IBR-1e).
    /// </summary>
    private static bool IsStrongAnchor(IngLineData lineData)
    {
        if (!TryGetBlockStartDate(lineData, out _))
            return false;

        string lineText = lineData.Text.Trim();
        return IngMonetaryExtractor.ExtractRightToLeft(lineText) is not null;
    }

    private static bool TryGetBlockStartDate(IngLineData lineData, out string? date)
    {
        PositionedWord[] words = lineData.Words;
        if (words.Length > 0)
        {
            PositionedWord firstWord = words[0];
            if (firstWord.Left <= FirstColumnMaxLeft
                && ExactDatePattern.IsMatch(firstWord.Text))
            {
                date = firstWord.Text;
                return true;
            }

            date = null;
            return false;
        }

        string trimmed = lineData.Text.Trim();
        if (DatePrefixPattern.IsMatch(trimmed))
        {
            date = trimmed[..10];
            return true;
        }

        date = null;
        return false;
    }

    private static void FlushBlock(
        List<IngBlock> blocks,
        string? date,
        List<IngLineData> lines)
    {
        if (date is null || lines.Count == 0)
            return;

        string fullText = string.Join(" ", lines.Select(l => l.Text.Trim()));
        blocks.Add(new IngBlock(date, fullText, [.. lines]));
    }
}

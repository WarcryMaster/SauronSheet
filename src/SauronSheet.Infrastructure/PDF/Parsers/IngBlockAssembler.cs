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
///   <item>Lines that do <em>not</em> begin with a date are continuation lines
///         and are appended to the most recent open block.</item>
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

        foreach (IngLineData lineData in lines)
        {
            if (TryGetBlockStartDate(lineData, out string? blockDate))
            {
                // Flush the open block (if any) before starting a new one
                FlushBlock(blocks, currentDate, currentLines);

                // Start a new block
                currentDate = blockDate;
                currentLines = [lineData];
            }
            else if (currentDate is not null)
            {
                // Continuation line — append to the open block
                currentLines.Add(lineData);
            }
            // Lines before the first date are silently ignored
        }

        // Flush the final open block
        FlushBlock(blocks, currentDate, currentLines);

        return blocks;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ────────────────────────────────────────────────────────────────────────

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

namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The result produced by <see cref="IngRawColumnExtractor.Extract"/>.
///
/// <list type="bullet">
///   <item><see cref="Category"/> — raw literal from the CATEGORÍA column (null when
///         no words land in that X zone).</item>
///   <item><see cref="SubCategory"/> — raw literal from the SUBCATEGORÍA column (null
///         when no words land in that X zone).</item>
///   <item><see cref="Description"/> — text from the DESCRIPCIÓN column plus any
///         continuation lines; null when the description zone is empty.</item>
/// </list>
///
/// Unlike the deleted taxonomy-based approach, this struct never invents category values:
/// it preserves PDF literals exactly or returns null (conservative fallback — IBR-3e).
/// </summary>
internal readonly record struct IngRawColumnResult(
    string? Category,
    string? SubCategory,
    string? Description);

/// <summary>
/// Geometry-first extractor that classifies <see cref="PositionedWord"/> entries from
/// an ING Bank block's anchor line into category, subcategory, and description columns
/// using the X-position thresholds derived from the page header.
///
/// Column assignment rules (IBR-3 geometry):
/// <list type="bullet">
///   <item>[0, CategoryStart)                    → ignored (date / F.VALOR zone)</item>
///   <item>[CategoryStart, SubCategoryStart)      → Category bucket</item>
///   <item>[SubCategoryStart, DescriptionStart)   → SubCategory bucket</item>
///   <item>[DescriptionStart, MonetaryZoneStart)  → Description bucket</item>
///   <item>[MonetaryZoneStart, ∞)                 → excluded (monetary amounts)</item>
/// </list>
///
/// When <see cref="IngColumnThresholds.MonetaryZoneStart"/> is null, all words at or
/// beyond <see cref="IngColumnThresholds.DescriptionStart"/> are treated as description
/// (no right boundary applied).
///
/// Continuation lines contribute their <see cref="IngLineData.Text"/> to the description
/// bucket regardless of X position — continuation rows do not have reliable column geometry.
///
/// Conservative fallback (IBR-3e): when the category bucket is empty, Category and
/// SubCategory are both null. Description may still be populated if description-zone
/// or continuation text is present.
///
/// This class is <c>internal</c> so it can be tested from
/// <c>SauronSheet.Infrastructure.Tests</c> without leaking to outer layers.
/// </summary>
internal static class IngRawColumnExtractor
{
    /// <summary>
    /// Extracts category, subcategory, and description from the positioned words of
    /// an ING Bank block anchor line plus optional continuation lines.
    /// </summary>
    /// <param name="anchorWords">
    /// Positioned words from the anchor (first) line of the block.
    /// Words are classified by their <see cref="PositionedWord.Left"/> X coordinate.
    /// </param>
    /// <param name="continuationLines">
    /// Optional continuation lines appended below the anchor.
    /// Their <see cref="IngLineData.Text"/> is appended to the description bucket.
    /// </param>
    /// <param name="thresholds">
    /// Column thresholds derived from the detected page header.
    /// </param>
    /// <returns>
    /// An <see cref="IngRawColumnResult"/> with the extracted values.
    /// Category and SubCategory are null when the respective column has no words.
    /// </returns>
    internal static IngRawColumnResult Extract(
        PositionedWord[] anchorWords,
        IngLineData[]? continuationLines,
        IngColumnThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(anchorWords);
        ArgumentNullException.ThrowIfNull(thresholds);

        var categoryWords    = new List<string>();
        var subCategoryWords = new List<string>();
        var descriptionParts = new List<string>();

        // Classify anchor words by X zone, preserving reading order
        var orderedAnchorWords = anchorWords.OrderBy(w => w.Left);

        foreach (PositionedWord word in orderedAnchorWords)
        {
            double x = word.Left;

            // Pre-category zone (date / F.VALOR column) — ignore
            if (x < thresholds.CategoryStart)
                continue;

            // Beyond monetary zone — exclude (monetary amounts should never contaminate text)
            if (thresholds.MonetaryZoneStart.HasValue && x >= thresholds.MonetaryZoneStart.Value)
                continue;

            if (x < thresholds.SubCategoryStart)
            {
                categoryWords.Add(word.Text);
            }
            else if (x < thresholds.DescriptionStart)
            {
                subCategoryWords.Add(word.Text);
            }
            else
            {
                descriptionParts.Add(word.Text);
            }
        }

        // Continuation lines: append their text to description (no X geometry available)
        if (continuationLines is { Length: > 0 })
        {
            foreach (IngLineData continuation in continuationLines)
            {
                string continuationText = continuation.Text.Trim();
                if (!string.IsNullOrEmpty(continuationText))
                    descriptionParts.Add(continuationText);
            }
        }

        // Assemble description from all collected parts
        string? description = NullifyIfEmpty(descriptionParts);

        // Conservative fallback: when no category signal, return null/null (IBR-3e)
        if (categoryWords.Count == 0)
            return new IngRawColumnResult(null, null, description);

        string? category    = NullifyIfEmpty(categoryWords);
        string? subCategory = subCategoryWords.Count > 0 ? NullifyIfEmpty(subCategoryWords) : null;

        return new IngRawColumnResult(category, subCategory, description);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Pure helper — joins a list of strings and returns null when the result
    // is empty or whitespace-only.
    // ────────────────────────────────────────────────────────────────────────

    private static string? NullifyIfEmpty(List<string> parts)
    {
        string joined = string.Join(" ", parts).Trim();
        return joined.Length == 0 ? null : joined;
    }
}

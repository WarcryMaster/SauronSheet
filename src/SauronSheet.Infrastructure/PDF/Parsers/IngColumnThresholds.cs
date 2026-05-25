namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Positioned word extracted from a PDF page — carries the word text and its
/// absolute X coordinate (left edge, in PDF points) on the page.
/// Used to segment ING Bank single-line transaction rows into columns.
/// </summary>
internal readonly record struct PositionedWord(string Text, double Left);

/// <summary>
/// Combines the reconstructed text of a PDF line with the position-aware words
/// that form it. <see cref="Text"/> is the whitespace-joined representation used
/// for pattern matching; <see cref="Words"/> preserve the original X positions
/// used for column segmentation.
/// </summary>
internal readonly record struct IngLineData(string Text, PositionedWord[] Words);

/// <summary>
/// Encapsulates the X-position thresholds that define the three data columns in
/// an ING Bank PDF statement page:
///
///   [CategoryStart .. SubCategoryStart)    →  Categoría column
///   [SubCategoryStart .. DescriptionStart) →  Subcategoría column
///   [DescriptionStart .. )                 →  Descripción / Concepto column
///
/// Thresholds are derived dynamically from the header line of each page
/// (words "CATEGORÍA" / "SUBCATEGORÍA" / "DESCRIPCIÓN" or "CONCEPTO") so the parser is
/// resilient to minor rendering differences across PDF batches.
///
/// All types in this file are <c>internal</c> and must not leak to the
/// Application or Domain layers.
/// </summary>
internal sealed class IngColumnThresholds
{
    // ────────────────────────────────────────────────────────────────────────
    // Header word identifiers (case-insensitive comparison)
    // ────────────────────────────────────────────────────────────────────────

    private const string HeaderCategory = "CATEGORÍA";
    private const string HeaderSubCategory = "SUBCATEGORÍA";
    private static readonly string[] HeaderDescriptionVariants =
    [
        "DESCRIPCIÓN",
        "DESCRIPCION",
        "CONCEPTO"
    ];

    // ────────────────────────────────────────────────────────────────────────
    // Properties
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Left X boundary of the Categoría column.</summary>
    public double CategoryStart { get; }

    /// <summary>Left X boundary of the Subcategoría column.</summary>
    public double SubCategoryStart { get; }

    /// <summary>Left X boundary of the Descripción/Concepto column.</summary>
    public double DescriptionStart { get; }

    private IngColumnThresholds(double categoryStart, double subCategoryStart, double descriptionStart)
    {
        CategoryStart = categoryStart;
        SubCategoryStart = subCategoryStart;
        DescriptionStart = descriptionStart;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an <see cref="IngColumnThresholds"/> instance from the positioned
    /// words of an ING Bank header line.
    ///
    /// The words "CATEGORÍA", "SUBCATEGORÍA", and one of the accepted description
    /// header variants (case-insensitive) are located; their Left X coordinates
    /// become the column boundaries.
    /// Returns <c>null</c> when any of the three header words is absent —
    /// the conservative fallback will apply for all rows on that page.
    /// </summary>
    public static IngColumnThresholds? FromHeaderWords(PositionedWord[] headerWords)
    {
        double? categoryX    = null;
        double? subCategoryX = null;
        double? descriptionX = null;

        foreach (var word in headerWords)
        {
            var text = word.Text;
            if (categoryX is null
                && text.Equals(HeaderCategory, StringComparison.OrdinalIgnoreCase))
            {
                categoryX = word.Left;
            }
            else if (subCategoryX is null
                && text.Equals(HeaderSubCategory, StringComparison.OrdinalIgnoreCase))
            {
                subCategoryX = word.Left;
            }
            else if (descriptionX is null
                && HeaderDescriptionVariants.Any(x => text.Equals(x, StringComparison.OrdinalIgnoreCase)))
            {
                descriptionX = word.Left;
            }
        }

        if (categoryX is null || subCategoryX is null || descriptionX is null)
            return null;

        return new IngColumnThresholds(categoryX.Value, subCategoryX.Value, descriptionX.Value);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Column assignment
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Assigns each positioned word to one of three buckets (Categoría,
    /// Subcategoría, Descripción) based on its Left X coordinate relative to
    /// the stored thresholds, then joins each bucket into a trimmed string.
    ///
    /// Returns <c>null</c> when the Categoría bucket is empty — this signals
    /// insufficient X signal for reliable column splitting (PCE-SLb fallback).
    /// A partially populated result (e.g., no Subcategoría words) is still
    /// returned as a non-null tuple (PCE-SLc).
    ///
    /// Words with Left &lt; <see cref="CategoryStart"/> (date / auxiliary columns)
    /// are silently ignored.
    /// </summary>
    public (string? Category, string? SubCategory, string? Description)?
        SplitWords(PositionedWord[] words)
    {
        var categoryWords    = new List<string>();
        var subCategoryWords = new List<string>();
        var descriptionWords = new List<string>();

        // Sort by X position to preserve reading order within each bucket
        var orderedWords = words.OrderBy(w => w.Left);

        foreach (var word in orderedWords)
        {
            var x = word.Left;

            if (x < CategoryStart)
            {
                // Pre-category zone (date / F.VALOR column) — ignore
                continue;
            }

            if (x < SubCategoryStart)
            {
                categoryWords.Add(word.Text);
            }
            else if (x < DescriptionStart)
            {
                subCategoryWords.Add(word.Text);
            }
            else
            {
                descriptionWords.Add(word.Text);
            }
        }

        // PCE-SLb: no category signal → conservative fallback
        if (categoryWords.Count == 0)
            return null;

        var category    = NullifyIfEmpty(string.Join(" ", categoryWords));
        var subCategory = NullifyIfEmpty(string.Join(" ", subCategoryWords));
        var description = NullifyIfEmpty(string.Join(" ", descriptionWords));

        return (category, subCategory, description);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helper
    // ────────────────────────────────────────────────────────────────────────

    private static string? NullifyIfEmpty(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}

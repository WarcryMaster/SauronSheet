namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Result produced by <see cref="IngControlledTaxonomy.ExtractLeftToRight"/>.
///
/// <list type="bullet">
///   <item><see cref="Category"/> — ING CATEGORÍA literal (known or raw; null when text is empty).</item>
///   <item><see cref="SubCategory"/> — ING SUBCATEGORÍA literal when matched (always null when
///         <see cref="IsRawOnly"/> is <see langword="true"/>).</item>
///   <item><see cref="Description"/> — Remaining block text after consuming category + subcategory
///         tokens (IBR-3c); null when nothing remains.</item>
///   <item><see cref="IsRawOnly"/> — <see langword="true"/> when no taxonomy prefix was matched
///         and the category value was derived from raw leading tokens (IBR-3b, PCE-1a).</item>
/// </list>
/// </summary>
internal readonly record struct IngTaxonomyResult(
    string? Category,
    string? SubCategory,
    string? Description,
    bool IsRawOnly);

/// <summary>
/// Ordered category/subcategory dictionary for ING Bank statements.
///
/// <b>Extraction rule (IBR-3):</b>
/// <list type="number">
///   <item>The method attempts a <em>longest-prefix-first</em> (L→R) match of the input text
///         against the known (CATEGORÍA, SUBCATEGORÍA) seed entries from the January-2025
///         ING fixture.</item>
///   <item>When a full (category + subcategory) prefix is matched, both fields are returned
///         and the remainder becomes the description.</item>
///   <item>When only a single-token category prefix is matched, SubCategory is <see langword="null"/>
///         and the remainder is the description (PCE-1c).</item>
///   <item>When <b>no prefix matches</b>, the extractor falls back to <em>raw mode</em>:
///         the first two leading tokens are treated as the raw CATEGORÍA literal, the remainder
///         as the description, and <see cref="IngTaxonomyResult.IsRawOnly"/> is set to
///         <see langword="true"/> (IBR-3b, PCE-1a).</item>
///   <item>When the input is null or whitespace, all fields are <see langword="null"/> and
///         <c>IsRawOnly</c> is <see langword="false"/> (PCE-1b).</item>
/// </list>
///
/// The seed is ordered by prefix length descending so that longer (more specific) prefixes
/// are attempted before shorter ones, enabling "Compras Online" to shadow plain "Compras".
///
/// This class is <c>internal</c> so it can be tested from
/// <c>SauronSheet.Infrastructure.Tests</c> without leaking to outer layers.
/// </summary>
internal static class IngControlledTaxonomy
{
    // ────────────────────────────────────────────────────────────────────────
    // January-2025 seed — ordered by prefix length descending (longest first)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Each entry maps a text prefix (category or category + subcategory) to the pair
    /// (Category, SubCategory). Ordered by <c>Prefix.Length</c> descending to ensure
    /// longest-match-first evaluation.
    /// </summary>
    private static readonly IReadOnlyList<(string Prefix, string Category, string? SubCategory)>
        Seed = BuildSeed();

    private static IReadOnlyList<(string Prefix, string Category, string? SubCategory)> BuildSeed()
    {
        // Raw entries — order here does not matter; they are sorted below.
        var entries = new List<(string Prefix, string Category, string? SubCategory)>
        {
            // ── Compras ──────────────────────────────────────────────────────
            ("Compras Online",           "Compras",         "Online"),
            ("Compras Ropa",             "Compras",         "Ropa"),
            ("Compras Electrónica",      "Compras",         "Electrónica"),
            ("Compras Electronica",      "Compras",         "Electrónica"),
            ("Compras",                  "Compras",         null),

            // ── Ocio ─────────────────────────────────────────────────────────
            ("Ocio Parking",             "Ocio",            "Parking"),
            ("Ocio Restaurantes",        "Ocio",            "Restaurantes"),
            ("Ocio Viajes",              "Ocio",            "Viajes"),
            ("Ocio Deportes",            "Ocio",            "Deportes"),
            ("Ocio",                     "Ocio",            null),

            // ── Nómina / payroll ──────────────────────────────────────────────
            ("Nómina",                   "Nómina",          null),
            ("Nomina",                   "Nómina",          null),

            // ── Bizum ────────────────────────────────────────────────────────
            ("Bizum",                    "Bizum",           null),

            // ── Transferencias ───────────────────────────────────────────────
            ("Transferencias Bizum",     "Transferencias",  "Bizum"),
            ("Transferencias",           "Transferencias",  null),

            // ── Recibos / utilities ───────────────────────────────────────────
            ("Recibos Agua Gas Luz",     "Recibos",         "Agua Gas Luz"),
            ("Recibos",                  "Recibos",         null),

            // ── Supermercados ─────────────────────────────────────────────────
            ("Supermercados",            "Supermercados",   null),

            // ── Seguros ───────────────────────────────────────────────────────
            ("Seguros",                  "Seguros",         null),

            // ── Comisiones ────────────────────────────────────────────────────
            ("Comisiones",               "Comisiones",      null),

            // ── Alimentación ─────────────────────────────────────────────────
            ("Alimentación",             "Alimentación",    null),
            ("Alimentacion",             "Alimentación",    null),

            // ── Salud ─────────────────────────────────────────────────────────
            ("Salud",                    "Salud",           null),

            // ── Hogar ─────────────────────────────────────────────────────────
            ("Hogar",                    "Hogar",           null),

            // ── Transporte ───────────────────────────────────────────────────
            ("Transporte",               "Transporte",      null),

            // ── Formación ────────────────────────────────────────────────────
            ("Formación",                "Formación",       null),
        };

        // Sort by prefix length descending: longer prefixes are attempted first.
        return entries
            .OrderByDescending(e => e.Prefix.Length)
            .ThenBy(e => e.Prefix, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Number of leading tokens taken as raw category when no match is found.
    // ING categories are typically 1–2 words; 2 is a safe upper bound.
    // ────────────────────────────────────────────────────────────────────────

    private const int RawCategoryMaxTokens = 2;

    // ────────────────────────────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts category, subcategory and description from the clean block text
    /// (after monetary tokens and the date prefix have been stripped) using a
    /// left-to-right, longest-prefix-first match against the January-2025 seed.
    ///
    /// Returns an <see cref="IngTaxonomyResult"/> with:
    /// <list type="bullet">
    ///   <item>Matched entries: <c>Category</c> + <c>SubCategory?</c> from seed; <c>IsRawOnly=false</c>.</item>
    ///   <item>Unmatched non-empty text: first ≤2 tokens as <c>Category</c>; <c>IsRawOnly=true</c>.</item>
    ///   <item>Empty/null input: all fields null; <c>IsRawOnly=false</c> (PCE-1b).</item>
    /// </list>
    /// </summary>
    /// <param name="text">
    /// Clean description text — block text with date prefix and monetary tokens already removed.
    /// May be null or empty when the block had no textual content beyond date and amounts.
    /// </param>
    internal static IngTaxonomyResult ExtractLeftToRight(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new IngTaxonomyResult(null, null, null, false);  // PCE-1b

        string normalized = text.Trim();

        // Longest-prefix-first match
        foreach ((string prefix, string category, string? subCategory) in Seed)
        {
            if (IsPrefix(normalized, prefix))
            {
                string remaining = ConsumePrefix(normalized, prefix);
                string? description = NullifyIfEmpty(remaining);

                return new IngTaxonomyResult(category, subCategory, description, false);
            }
        }

        // ── No match — RawOnly fallback ──────────────────────────────────────
        // Take the first RawCategoryMaxTokens tokens as raw category literal.
        // This preserves the CATEGORÍA column content from the PDF (IBR-3b, PCE-1a).
        return BuildRawOnlyResult(normalized);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="text"/> starts with
    /// <paramref name="prefix"/> followed by a space or is exactly equal to it
    /// (case-insensitive).
    /// </summary>
    private static bool IsPrefix(string text, string prefix)
    {
        if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            return true;

        if (text.Length > prefix.Length &&
            text[prefix.Length] == ' ' &&
            text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the prefix and leading space from the text, returning the remainder.
    /// </summary>
    private static string ConsumePrefix(string text, string prefix)
    {
        if (text.Length <= prefix.Length)
            return string.Empty;

        return text[(prefix.Length + 1)..].Trim(); // +1 skips the space separator
    }

    /// <summary>
    /// Builds a RawOnly result by splitting the text and taking up to
    /// <see cref="RawCategoryMaxTokens"/> tokens as the raw category.
    /// </summary>
    private static IngTaxonomyResult BuildRawOnlyResult(string text)
    {
        string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
            return new IngTaxonomyResult(null, null, null, false);

        int catCount = Math.Min(RawCategoryMaxTokens, tokens.Length);
        string rawCategory = string.Join(" ", tokens[..catCount]);

        string? description = tokens.Length > catCount
            ? string.Join(" ", tokens[catCount..])
            : null;

        return new IngTaxonomyResult(rawCategory, null, NullifyIfEmpty(description), true);
    }

    private static string? NullifyIfEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}

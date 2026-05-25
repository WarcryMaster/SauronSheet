namespace SauronSheet.Application.Services;

using System.Globalization;
using System.Text;

/// <summary>
/// Static normalizer for category and subcategory names.
/// Single source of truth for deduplication keys stored in the normalized_name column.
///
/// Algorithm: trim → normalize to FormD → strip NonSpacingMark characters → lowercase.
/// Output is deterministic: same string always produces the same key across CLR versions.
///
/// Design decision D2: Application concern — no Domain dependency; pure static function.
/// DB stores output; never computes independently.
/// </summary>
public static class CategoryNormalizer
{
    /// <summary>
    /// Normalizes a category or subcategory name to a deduplication key.
    /// Returns null for null, empty, or whitespace-only input.
    /// </summary>
    /// <param name="value">Raw category name from PDF or user input.</param>
    /// <returns>
    /// Lowercase, diacritic-stripped, trimmed key; or null if input is null/whitespace.
    /// </returns>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        // Decompose into base characters + combining marks (FormD)
        var decomposed = trimmed.Normalize(NormalizationForm.FormD);

        // Filter out all NonSpacingMark characters (diacritics, accents, tildes)
        var stripped = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                stripped.Append(ch);
        }

        // Re-compose to FormC and fold to lowercase
        return stripped.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}

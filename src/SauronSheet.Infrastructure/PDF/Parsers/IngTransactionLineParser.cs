namespace SauronSheet.Infrastructure.PDF.Parsers;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extracts category, subcategory, and description from the text lines of a
/// multi-line ING Bank transaction block.
///
/// Position-first contract (design D5):
///   textLines[0] → category  (raw literal from PDF)
///   textLines[1] → subcategory (if count >= 2)
///   textLines[2+] → description (joined with space)
///
/// NO known-category list is used. Any literal from the PDF is preserved as-is
/// regardless of whether it appeared in previous closed lists.
/// This class is internal so it can be unit-tested from SauronSheet.Infrastructure.Tests.
/// </summary>
internal static class IngTransactionLineParser
{
    /// <summary>
    /// Extracts category, subcategory, and description from an ordered list of
    /// text lines representing the non-date, non-numeric content of one transaction row.
    ///
    /// Callers are responsible for stripping date lines and trailing numeric-only
    /// lines (amount / balance) before calling this method.
    /// </summary>
    /// <param name="textLines">
    /// Ordered text lines from the transaction block, with date and numeric lines removed.
    /// May be empty.
    /// </param>
    /// <returns>
    /// (category, subCategory, description) — any element may be null if the
    /// corresponding line is absent or whitespace-only.
    /// </returns>
    internal static (string? category, string? subCategory, string? description)
        ExtractFromMultiLine(IReadOnlyList<string> textLines)
    {
        if (textLines.Count == 0)
            return (null, null, null);

        string? category    = Nullify(textLines[0]);
        string? subCategory = textLines.Count >= 2 ? Nullify(textLines[1]) : null;
        string? description = null;

        if (textLines.Count >= 3)
        {
            var joined = string.Join(" ", textLines.Skip(2).Select(l => l.Trim()));
            description = Nullify(joined);
        }

        return (category, subCategory, description);
    }

    private static string? Nullify(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

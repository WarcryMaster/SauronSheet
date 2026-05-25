namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Text.RegularExpressions;

/// <summary>
/// Result produced by <see cref="IngMonetaryExtractor.ExtractRightToLeft"/>.
///
/// <list type="bullet">
///   <item><see cref="Amount"/> — normalized importe (e.g., <c>"-12.99"</c>).</item>
///   <item><see cref="Balance"/> — normalized saldo (e.g., <c>"1234.56"</c>).</item>
///   <item><see cref="CleanText"/> — block text with the two trailing monetary tokens removed;
///         fed to the downstream taxonomy extractor.</item>
/// </list>
///
/// The ING <c>COMENTARIO</c> column is contractually always empty; this type
/// intentionally carries no <c>Comment</c> property (IBR-2b).
/// </summary>
internal readonly record struct IngMonetaryResult(
    string Amount,
    string Balance,
    string CleanText);

/// <summary>
/// Pure static helper that isolates the <em>importe</em> and <em>saldo</em>
/// monetary tokens from an ING Bank logical block using a right-to-left scan.
///
/// <b>Extraction rule (IBR-2):</b>
/// <list type="bullet">
///   <item>The <em>last</em> monetary token in the block text is <em>saldo</em>.</item>
///   <item>The <em>penultimate</em> monetary token is <em>importe</em>.</item>
///   <item>Both values are normalized to <c>dot-decimal, no-thousands</c> format
///         via the same logic as the existing <c>NormalizeAmount</c> helper.</item>
///   <item>If fewer than two isolated monetary tokens are found, <see langword="null"/>
///         is returned (IBR-4a fallback conservador).</item>
/// </list>
///
/// A monetary token is defined as a whitespace-delimited token whose text
/// matches the pattern <c>^-?\d[\d.,]*[,\.]\d{2}$</c> — i.e., it ends in
/// exactly two decimal digits after a comma or period separator.
/// This intentionally excludes pure integers (e.g., years in dates) and
/// slash-separated date strings.
///
/// This class is <c>internal</c> so it can be tested from
/// <c>SauronSheet.Infrastructure.Tests</c> without leaking to outer layers.
/// </summary>
internal static class IngMonetaryExtractor
{
    // Matches a monetary token: optional minus, digits, optional thousands-separators,
    // ending with exactly 2 decimal digits after comma or period.
    // Examples: "-12,99"  "1.234,56"  "3.200,00"  "0,82"
    private static readonly Regex MonetaryTokenPattern = new(
        @"^-?\d[\d.,]*[,\.]\d{2}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Scans <paramref name="blockText"/> right-to-left to isolate the two
    /// trailing monetary tokens (importe + saldo) of an ING statement row.
    /// </summary>
    /// <param name="blockText">
    /// The joined full text of a logical <see cref="IngBlock"/>. Must not be
    /// <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IngMonetaryResult"/> with normalized Amount, Balance, and
    /// CleanText (block text minus the two trailing tokens), or
    /// <see langword="null"/> when fewer than two monetary tokens are isolatable
    /// (IBR-4a).
    /// </returns>
    internal static IngMonetaryResult? ExtractRightToLeft(string blockText)
    {
        ArgumentNullException.ThrowIfNull(blockText);

        if (string.IsNullOrWhiteSpace(blockText))
            return null;

        string[] tokens = blockText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Walk from the right collecting monetary tokens
        int balanceIndex = -1;
        int amountIndex = -1;

        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            if (!MonetaryTokenPattern.IsMatch(tokens[i]))
                break;   // stop at first non-monetary token from the right

            if (balanceIndex == -1)
            {
                balanceIndex = i;
            }
            else if (amountIndex == -1)
            {
                amountIndex = i;
                break;   // we have both — no need to continue
            }
        }

        // IBR-4a: fewer than 2 monetary tokens → fallback conservador
        if (balanceIndex == -1 || amountIndex == -1)
            return null;

        string rawBalance = tokens[balanceIndex];
        string rawAmount = tokens[amountIndex];

        // Build CleanText: all tokens before the amount index (inclusive of date and description)
        string cleanText = string.Join(" ", tokens[..amountIndex]);

        return new IngMonetaryResult(
            NormalizeAmount(rawAmount),
            NormalizeAmount(rawBalance),
            cleanText);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Amount normalization (mirrors NormalizeAmount in IngBankPdfParser)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Normalizes a raw monetary string to <c>dot-decimal, no-thousands</c> format.
    /// Supports both European (<c>1.234,56</c>) and Anglo (<c>1,234.56</c>) formats.
    /// </summary>
    private static string NormalizeAmount(string raw)
    {
        if (!raw.Contains(',') && !raw.Contains('.'))
            return raw;

        if (raw.Contains(',') && raw.Contains('.'))
        {
            int lastComma = raw.LastIndexOf(',');
            int lastDot = raw.LastIndexOf('.');

            if (lastComma > lastDot)
            {
                // European: 1.234,56 → 1234.56
                return raw.Replace(".", string.Empty, StringComparison.Ordinal)
                          .Replace(",", ".", StringComparison.Ordinal);
            }
            else
            {
                // Anglo: 1,234.56 → 1234.56
                return raw.Replace(",", string.Empty, StringComparison.Ordinal);
            }
        }

        // Only comma → European decimal: -12,99 → -12.99
        if (raw.Contains(','))
            return raw.Replace(",", ".", StringComparison.Ordinal);

        // Only dot → already dot-decimal
        return raw;
    }
}

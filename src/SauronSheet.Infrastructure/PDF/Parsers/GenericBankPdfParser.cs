namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SauronSheet.Application.Common.Models;
using SauronSheet.Application.Interfaces;
using UglyToad.PdfPig;

/// <summary>
/// Generic bank PDF parser using PdfPig (Apache 2.0, native .NET).
/// Replaces iTextSharp (AGPL, Java-based legacy).
/// Reconstructs lines from word bounding boxes and normalizes currency to ISO 4217 (max 3 chars).
/// Heuristic parsing: expects format "DD/MM/YYYY Description AMOUNT [CURRENCY]".
/// </summary>
public class GenericBankPdfParser : IPdfParser
{
    private static readonly HashSet<string> KnownCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "EUR", "USD", "GBP", "CHF", "JPY", "CAD", "AUD" };

    public async Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        var rows = new List<RawTransactionRow>();

        // PdfPig requires a byte array — read stream fully first
        byte[] pdfBytes;
        using (var ms = new MemoryStream())
        {
            await pdfStream.CopyToAsync(ms);
            pdfBytes = ms.ToArray();
        }

        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var rowNumber = 0;

            foreach (var page in document.GetPages())
            {
                try
                {
                    // Reconstruct lines by grouping words on the same vertical position (Y)
                    var lines = page.GetWords()
                        .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                        .OrderByDescending(g => g.Key)
                        .Select(g =>
                            string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));

                    foreach (var line in lines)
                    {
                        rowNumber++;

                        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length < 3)
                            continue;

                        var dateRaw = parts[0];
                        var lastPart = parts[^1];

                        string? currencyRaw;
                        string? amountRaw;
                        string? descriptionRaw;

                        // Detect if last token is a currency code (≤3 letters, alpha only)
                        if (IsCurrencyCode(lastPart) && parts.Length >= 4)
                        {
                            currencyRaw = NormalizeCurrency(lastPart);
                            amountRaw = parts[^2];
                            descriptionRaw = string.Join(" ", parts[1..^2]);
                        }
                        else
                        {
                            // No currency suffix — assume EUR, last part is amount
                            currencyRaw = "EUR";
                            amountRaw = lastPart;
                            descriptionRaw = parts.Length > 2
                                ? string.Join(" ", parts[1..^1])
                                : null;
                        }

                        rows.Add(new RawTransactionRow(
                            rowNumber,
                            dateRaw,
                            descriptionRaw,
                            amountRaw,
                            currencyRaw));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error parsing page {page.Number}: {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (
            ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "PDF cannot be read. It may be password-protected or encrypted.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Unexpected error while parsing PDF: {ex.Message}", ex);
        }

        return rows;
    }

    private static bool IsCurrencyCode(string value) =>
        value.Length <= 3 && value.All(char.IsLetter);

    private static string NormalizeCurrency(string raw)
    {
        var upper = raw.ToUpperInvariant().Trim();
        return KnownCurrencies.Contains(upper) ? upper : "EUR";
    }
}


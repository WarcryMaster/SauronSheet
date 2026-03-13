namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
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

        SentrySdk.Logger?.LogInfo("GenericBankPdfParser: parsing PDF ({0} bytes)", pdfBytes.Length);

        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var rowNumber = 0;

            foreach (var page in document.GetPages())
            {
                SentrySdk.Logger?.LogDebug("GenericBankPdfParser: processing page {0}", page.Number);
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
                            null,               // category
                            null,               // subCategory
                            descriptionRaw,
                            null,               // comment
                            NormalizeAmount(amountRaw),
                            null,               // balance
                            currencyRaw));
                    }
                }
                catch (Exception ex)
                {
                    SentrySdk.Logger?.LogWarning("GenericBankPdfParser: page {0} parse error — {1}", page.Number, ex.Message);
                    Console.WriteLine($"Warning: Error parsing page {page.Number}: {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (
            ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            SentrySdk.Logger?.LogError("GenericBankPdfParser: PDF is encrypted or password-protected");
            throw new InvalidOperationException(
                "PDF cannot be read. It may be password-protected or encrypted.", ex);
        }
        catch (Exception ex)
        {
            SentrySdk.Logger?.LogError("GenericBankPdfParser: unexpected error — {0}", ex.Message);
            throw new InvalidOperationException(
                $"Unexpected error while parsing PDF: {ex.Message}", ex);
        }

        SentrySdk.Logger?.LogInfo("GenericBankPdfParser: parsed {0} raw rows from PDF", rows.Count);
        return rows;
    }

    private static bool IsCurrencyCode(string value) =>
        value.Length <= 3 && value.All(char.IsLetter);

    private static string NormalizeCurrency(string raw)
    {
        var upper = raw.ToUpperInvariant().Trim();
        return KnownCurrencies.Contains(upper) ? upper : "EUR";
    }

    /// <summary>
    /// Normaliza el formato del importe: soporta ambos formatos (europeo con coma decimal y anglo con punto decimal).
    /// Convierte a formato estándar con punto decimal y sin separadores de miles.
    /// Ejemplos: "1,246.74" → "1246.74", "1.246,74" → "1246.74", "0.82" → "0.82", "0,82" → "0.82"
    /// </summary>
    private static string? NormalizeAmount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount))
            return null;

        amount = amount.Trim();

        // Si no tiene punto ni coma, retornar como está
        if (!amount.Contains(',') && !amount.Contains('.'))
            return amount;

        // Si tiene ambos separadores, detectar cuál es decimal y cuál es de miles
        if (amount.Contains(',') && amount.Contains('.'))
        {
            // El último separador (más a la derecha) es el decimal
            var lastCommaIndex = amount.LastIndexOf(',');
            var lastDotIndex = amount.LastIndexOf('.');

            string normalized;
            if (lastCommaIndex > lastDotIndex)
            {
                // Coma es decimal: "1.246,74" → "1246.74"
                normalized = amount
                    .Replace(".", string.Empty)  // Quitar separador de miles
                    .Replace(",", ".");          // Coma a punto decimal
            }
            else
            {
                // Punto es decimal: "1,246.74" → "1246.74"
                normalized = amount.Replace(",", string.Empty);  // Quitar separador de miles
            }

            return normalized;
        }

        // Si solo tiene coma, es probablemente decimal (formato europeo)
        if (amount.Contains(',') && !amount.Contains('.'))
        {
            return amount.Replace(",", ".");
        }

        // Si solo tiene punto, es probablemente decimal o parte de "1.234" sin decimales
        // Retornar como está (ya tiene punto decimal)
        return amount;
    }
}


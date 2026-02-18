namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SauronSheet.Application.Common.Models;
using SauronSheet.Application.Interfaces;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

/// <summary>
/// Generic bank PDF parser implementation.
/// CRITICAL FIX NC-3: Error handling for encoding issues, password-protected PDFs.
/// </summary>
public class GenericBankPdfParser : IPdfParser
{
    public async Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        var rows = new List<RawTransactionRow>();

        try
        {
            var reader = new PdfReader(pdfStream);
            var rowNumber = 0;

            for (int pageNum = 1; pageNum <= reader.NumberOfPages; pageNum++)
            {
                try
                {
                    var text = PdfTextExtractor.GetTextFromPage(reader, pageNum);
                    var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        rowNumber++;

                        // Heuristic parsing: assumes format "DD/MM/YYYY Description AMOUNT EUR"
                        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length < 3)
                            continue; // Skip invalid lines

                        var dateRaw = parts[0];
                        var amountRaw = parts.Length >= 2 ? parts[^2] : null; // Second-to-last part
                        var currencyRaw = parts.Length >= 1 ? parts[^1] : null; // Last part
                        var descriptionRaw = parts.Length > 2 
                            ? string.Join(" ", parts[1..^2]) 
                            : null; // Middle parts

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
                    // CRITICAL FIX NC-3: Log page-level errors but continue processing
                    Console.WriteLine($"Warning: Error parsing page {pageNum}: {ex.Message}");
                }
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("encrypted"))
        {
            // CRITICAL FIX NC-3: Handle password-protected PDFs
            throw new InvalidOperationException(
                "PDF cannot be read. It may be password-protected, corrupted, or use an unsupported format.", 
                ex);
        }
        catch (System.Text.DecoderFallbackException ex)
        {
            // CRITICAL FIX NC-3: Handle encoding errors (UTF-16, scanned PDFs)
            throw new InvalidOperationException(
                "PDF contains encoding issues. It may be scanned or use unsupported character encoding.", 
                ex);
        }
        catch (Exception ex)
        {
            // CRITICAL FIX NC-3: General error handling
            throw new InvalidOperationException(
                $"Unexpected error while parsing PDF: {ex.Message}", 
                ex);
        }

        return rows;
    }
}


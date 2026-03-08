namespace SauronSheet.Infrastructure.PDF.Parsers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using SauronSheet.Application.Common.Models;
using SauronSheet.Application.Interfaces;

/// <summary>
/// Adaptive PDF parser that automatically detects the bank format and uses the appropriate parser.
/// Strategy: Try ING format first, fall back to generic parser if detection fails.
/// </summary>
public class AdaptivePdfParser : IPdfParser
{
    private readonly IngBankPdfParser _ingParser;
    private readonly GenericBankPdfParser _genericParser;

    public AdaptivePdfParser(IngBankPdfParser ingParser, GenericBankPdfParser genericParser)
    {
        _ingParser = ingParser ?? throw new ArgumentNullException(nameof(ingParser));
        _genericParser = genericParser ?? throw new ArgumentNullException(nameof(genericParser));
    }

    /// <summary>
    /// Attempts to parse PDF by detecting bank format.
    /// 1. Tries ING Bank format (looks for "F. VALOR", "CATEGORÍA" headers)
    /// 2. Falls back to generic parser if ING detection fails
    /// </summary>
    public async Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream)
    {
        try
        {
            // Read stream into memory (needed for multiple parse attempts)
            var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Detect bank format by attempting ING parser first
            var isIngFormat = await IsIngFormatAsync(memoryStream);

            if (isIngFormat)
            {
                SentrySdk.Logger?.LogInfo("AdaptivePdfParser: detected ING Bank format, using IngBankPdfParser");
                memoryStream.Seek(0, SeekOrigin.Begin);
                return await _ingParser.ParseAsync(memoryStream);
            }

            // Fall back to generic parser
            SentrySdk.Logger?.LogInfo("AdaptivePdfParser: ING format not detected, using GenericBankPdfParser");
            memoryStream.Seek(0, SeekOrigin.Begin);
            return await _genericParser.ParseAsync(memoryStream);
        }
        catch (Exception ex)
        {
            SentrySdk.Logger?.LogError("AdaptivePdfParser: parse error — {0}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Checks if PDF matches ING Bank format by looking for characteristic headers.
    /// </summary>
    private async Task<bool> IsIngFormatAsync(MemoryStream pdfStream)
    {
        try
        {
            using var ms = new MemoryStream(pdfStream.ToArray());
            return await _ingParser.ParseAsync(ms) is { Count: > 0 };
        }
        catch
        {
            // ING format detection failed, not ING format
            return false;
        }
    }
}

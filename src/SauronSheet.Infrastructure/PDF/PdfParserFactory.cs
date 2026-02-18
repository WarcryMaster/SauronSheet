namespace SauronSheet.Infrastructure.PDF;

using Application.Interfaces;
using Parsers;

/// <summary>
/// Factory for creating PDF parser instances.
/// Strategy pattern: supports bank-specific parsers.
/// </summary>
public class PdfParserFactory
{
    /// <summary>
    /// Creates a PDF parser instance.
    /// </summary>
    /// <param name="bankIdentifier">Optional bank identifier for specific parser</param>
    /// <returns>PDF parser implementation</returns>
    public IPdfParser CreateParser(string? bankIdentifier = null)
    {
        // Strategy pattern: return bank-specific parser based on identifier
        // Default: GenericBankPdfParser
        return bankIdentifier switch
        {
            // TODO Phase 3+: Add bank-specific parsers
            // "santander" => new SantanderPdfParser(),
            // "bbva" => new BbvaPdfParser(),
            _ => new GenericBankPdfParser()
        };
    }
}

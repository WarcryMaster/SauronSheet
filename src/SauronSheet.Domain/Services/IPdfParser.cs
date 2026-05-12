namespace SauronSheet.Domain.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using ValueObjects;

/// <summary>
/// Contract for PDF parsing.
/// Implementations parse bank statement PDFs and extract raw transaction rows.
/// </summary>
public interface IPdfParser
{
    Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream);
}

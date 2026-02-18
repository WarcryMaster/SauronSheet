namespace SauronSheet.Application.Interfaces;

using Common.Models;

public interface IPdfParser
{
    Task<List<RawTransactionRow>> ParseAsync(Stream pdfStream);
}

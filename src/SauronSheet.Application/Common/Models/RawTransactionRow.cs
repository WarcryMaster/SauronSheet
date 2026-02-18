namespace SauronSheet.Application.Common.Models;

public record RawTransactionRow(
    int RowNumber,
    string? DateRaw,
    string? DescriptionRaw,
    string? AmountRaw,
    string? CurrencyRaw = null);

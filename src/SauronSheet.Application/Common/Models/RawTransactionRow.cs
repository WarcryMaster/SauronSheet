namespace SauronSheet.Application.Common.Models;

public record RawTransactionRow(
    int RowNumber,
    string? Date,
    string? Category,
    string? SubCategory,
    string? Description,
    string? Comment,
    string? Amount,
    string? Balance,
    string? Currency = "EUR"
);

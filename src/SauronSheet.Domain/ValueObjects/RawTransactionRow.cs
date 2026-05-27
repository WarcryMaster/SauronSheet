namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Raw transaction row extracted from a bank statement file (Excel or other supported formats).
/// All fields are strings because they come from unparsed statement data.
/// The handler is responsible for converting to domain types with proper validation.
/// </summary>
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

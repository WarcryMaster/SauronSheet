namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Raw transaction row extracted from a PDF bank statement.
/// All fields are strings because they come from unparsed PDF text.
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

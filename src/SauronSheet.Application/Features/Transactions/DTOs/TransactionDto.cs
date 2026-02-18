namespace SauronSheet.Application.Features.Transactions.DTOs;

public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid? CategoryId,
    string? CategoryName,
    string? ImportedFrom,
    DateTime CreatedAt);

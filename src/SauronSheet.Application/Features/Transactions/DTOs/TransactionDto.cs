namespace SauronSheet.Application.Features.Transactions.DTOs;

/// <summary>
/// Data Transfer Object for Transaction.
/// Maps to/from the domain Transaction entity for API consumption.
/// Includes bank resolution data (BankCategory, BankSubcategory, SubcategoryId,
/// SubcategoryName, CategorySource) for import and categorization flows.
/// </summary>
public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid? CategoryId,
    string? CategoryName,
    string? ImportedFrom,
    DateTime CreatedAt,
    string? BankCategory = null,
    string? BankSubcategory = null,
    string? SubcategoryId = null,
    string? SubcategoryName = null,
    string CategorySource = "Legacy");

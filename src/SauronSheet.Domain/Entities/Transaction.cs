namespace SauronSheet.Domain.Entities;

using System;
using ValueObjects;
using Common;

/// <summary>
/// Transaction aggregate root.
/// Represents a financial transaction (income or expense).
/// </summary>
public class Transaction : AggregateRoot<TransactionId>
{
    public UserId UserId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public string? ImportedFrom { get; private set; }

    /// <summary>Raw bank category from bank statement parser, unmodified.</summary>
    public string? BankCategory { get; private set; }

    /// <summary>Raw bank subcategory from bank statement parser, unmodified.</summary>
    public string? BankSubcategory { get; private set; }

    /// <summary>Resolved subcategory, if any.</summary>
    public SubcategoryId? SubcategoryId { get; private set; }

    /// <summary>Source of the category assignment (Legacy, RawOnly, AutoMatched, UserOverride).</summary>
    public CategorySource CategorySource { get; private set; }

    /// <summary>Account balance at time of transaction. Used for duplicate detection.</summary>
    public decimal? Balance { get; private set; }

    /// <summary>
    /// Constructor for Transaction aggregate root.
    /// The first 6 params match the original signature for backward compatibility.
    /// New params (bankCategory, bankSubcategory, subcategoryId, categorySource, balance) default
    /// to null/Legacy so existing call sites continue to work without changes.
    /// </summary>
    public Transaction(
        TransactionId id,
        UserId userId,
        Money amount,
        DateTime date,
        string description,
        CategoryId? categoryId = null,
        string? importedFrom = null,
        string? bankCategory = null,
        string? bankSubcategory = null,
        SubcategoryId? subcategoryId = null,
        CategorySource categorySource = CategorySource.Legacy,
        decimal? balance = null)
        : base(id)
    {
        if (userId == null)
            throw new ArgumentNullException(nameof(userId));
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        UserId = userId;
        Amount = amount;
        Date = date;
        Description = description;
        CategoryId = categoryId;
        ImportedFrom = importedFrom;
        BankCategory = bankCategory;
        BankSubcategory = bankSubcategory;
        SubcategoryId = subcategoryId;
        CategorySource = categorySource;
        Balance = balance;
    }

    /// <summary>
    /// Categorize or uncategorize the transaction from user action (UI).
    /// When assigning a category, source is set to UserOverride.
    /// When clearing (null), the existing source is preserved.
    /// </summary>
    public void Categorize(CategoryId? categoryId)
    {
        CategoryId = categoryId;
        if (categoryId != null)
            CategorySource = CategorySource.UserOverride;
        // When categoryId is null, keep existing CategorySource
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Categorize with full resolution data (used by the resolution service).
    /// Sets category, subcategory, and source in one call.
    /// </summary>
    public void Categorize(CategoryId? categoryId, SubcategoryId? subcategoryId, CategorySource source)
    {
        CategoryId = categoryId;
        SubcategoryId = subcategoryId;
        CategorySource = source;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the transaction description.
    /// </summary>
    public void UpdateDescription(string newDescription)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("Description is required.", nameof(newDescription));

        Description = newDescription;
        UpdatedAt = DateTime.UtcNow;
    }
}

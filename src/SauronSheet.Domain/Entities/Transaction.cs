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

    public Transaction(
        TransactionId id,
        UserId userId,
        Money amount,
        DateTime date,
        string description,
        CategoryId? categoryId = null,
        string? importedFrom = null)
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
    }

    /// <summary>
    /// Categorize or uncategorize the transaction.
    /// </summary>
    public void Categorize(CategoryId? categoryId)
    {
        CategoryId = categoryId;
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

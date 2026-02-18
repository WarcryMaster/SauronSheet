namespace SauronSheet.Domain.Entities;

using System;
using ValueObjects;
using Common;

/// <summary>
/// Category aggregate root.
/// Represents an expense or income category (system default or user-defined).
/// </summary>
public class Category : AggregateRoot<CategoryId>
{
    public UserId UserId { get; private set; }
    public string Name { get; private set; }
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public bool IsSystemDefault { get; private set; }

    /// <summary>
    /// Private constructor for system defaults (factory method only).
    /// </summary>
    private Category(
        CategoryId id,
        UserId userId,
        string name,
        string? color,
        string? icon,
        bool isSystemDefault)
        : base(id)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Color = color;
        Icon = icon;
        IsSystemDefault = isSystemDefault;
    }

    /// <summary>
    /// Public constructor for user-defined categories.
    /// </summary>
    public Category(
        CategoryId id,
        UserId userId,
        string name,
        string? color = null,
        string? icon = null)
        : this(id, userId, name, color, icon, isSystemDefault: false)
    {
    }

    /// <summary>
    /// Factory method to create a system default category.
    /// </summary>
    public static Category CreateSystemDefault(CategoryId id, UserId userId, string name)
    {
        return new Category(id, userId, name, null, null, isSystemDefault: true);
    }

    /// <summary>
    /// Rename the category.
    /// Throws if trying to rename a system default category.
    /// </summary>
    public void Rename(string newName)
    {
        if (IsSystemDefault)
            throw new InvalidOperationException("Cannot rename a system default category.");

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name is required.", nameof(newName));

        if (Name == newName)
            return; // No-op if same name

        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if the category can be deleted.
    /// </summary>
    public bool CanDelete(bool hasActiveTransactions = false)
    {
        return !IsSystemDefault && !hasActiveTransactions;
    }
}

namespace SauronSheet.Domain.Entities;

using System;
using ValueObjects;
using Common;
using Exceptions;

/// <summary>
/// Category aggregate root.
/// Represents an expense or income category (system default or user-defined).
/// Enforces invariants: system categories immutable, names unique per user, delete guarded by transactions.
/// </summary>
public class Category : AggregateRoot<CategoryId>
{
    public UserId UserId { get; private set; }
    public CategoryName Name { get; private set; }
    public CategoryType Type { get; private set; }
    public ColorHex Color { get; private set; }
    public string IconName { get; private set; }
    public bool IsSystemDefault { get; private set; }

    /// <summary>
    /// Private constructor (use factory methods instead).
    /// </summary>
    private Category(
        CategoryId id,
        UserId userId,
        CategoryName name,
        CategoryType type,
        ColorHex color,
        string iconName,
        bool isSystemDefault)
        : base(id)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Color = color ?? throw new ArgumentNullException(nameof(color));
        IconName = iconName ?? throw new ArgumentNullException(nameof(iconName));
        IsSystemDefault = isSystemDefault;
    }

    /// <summary>
    /// Public constructor for user-defined custom categories.
    /// </summary>
    public Category(
        CategoryId id,
        UserId userId,
        CategoryName name,
        CategoryType type,
        ColorHex color,
        string iconName)
        : this(
            id,
            userId,
            name,
            type,
            color,
            iconName,
            isSystemDefault: false)
    {
    }

    /// <summary>
    /// Factory method to create a system default category.
    /// System categories are immutable and global (owned by system, not a specific user).
    /// </summary>
    public static Category CreateSystemDefault(
        CategoryId id,
        UserId userId,
        CategoryName name,
        CategoryType type,
        ColorHex color,
        string iconName)
    {
        return new Category(
            id,
            userId,
            name,
            type,
            color,
            iconName,
            isSystemDefault: true);
    }

    /// <summary>
    /// Update category properties (name, color, icon).
    /// Type and IsSystemDefault cannot be changed.
    /// Throws if trying to update a system default category.
    /// </summary>
    public void Update(CategoryName newName, ColorHex newColor, string newIconName)
    {
        if (IsSystemDefault)
        {
            throw new DomainException("System default categories cannot be modified.");
        }

        if (newName == null)
        {
            throw new ArgumentNullException(nameof(newName));
        }

        if (newColor == null)
        {
            throw new ArgumentNullException(nameof(newColor));
        }

        if (string.IsNullOrWhiteSpace(newIconName))
        {
            throw new ArgumentNullException(nameof(newIconName));
        }

        Name = newName;
        Color = newColor;
        IconName = newIconName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if the category can be deleted.
    /// Returns false if it's a system default or has active transactions.
    /// </summary>
    public bool CanDelete(bool hasActiveTransactions = false)
    {
        return !IsSystemDefault && !hasActiveTransactions;
    }
}

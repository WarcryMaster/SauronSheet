namespace SauronSheet.Domain.Entities;

using System;
using ValueObjects;
using Common;
using Exceptions;

/// <summary>
/// Category aggregate root.
/// Represents an expense or income category (system default or user-defined).
/// Enforces invariants: system categories immutable, names unique per user, delete guarded by transactions.
/// Feature 3: Supports nullable UserId for system/global categories (user_id = NULL in database).
/// </summary>
public class Category : AggregateRoot<CategoryId>
{
    public UserId? UserId { get; private set; }
    public CategoryName Name { get; private set; }
    public CategoryType Type { get; private set; }
    public ColorHex Color { get; private set; }
    public string IconName { get; private set; }
    public bool IsSystemDefault { get; private set; }

    /// <summary>
    /// Private constructor (use factory methods instead).
    /// Accepts nullable UserId for system categories.
    /// </summary>
    private Category(
        CategoryId id,
        UserId? userId,
        CategoryName name,
        CategoryType type,
        ColorHex color,
        string iconName,
        bool isSystemDefault)
        : base(id)
    {
        // Domain invariant: NULL user_id requires IsSystemDefault=true
        if (userId == null && !isSystemDefault)
        {
            throw new DomainException("Categories with null UserId must be marked as system defaults.");
        }

        UserId = userId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Color = color ?? throw new ArgumentNullException(nameof(color));
        IconName = iconName ?? throw new ArgumentNullException(nameof(iconName));
        IsSystemDefault = isSystemDefault;
    }

    /// <summary>
    /// Public constructor for user-defined custom categories.
    /// Requires non-null userId for user-scoped categories.
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
            userId ?? throw new ArgumentNullException(nameof(userId)),
            name,
            type,
            color,
            iconName,
            isSystemDefault: false)
    {
    }

    /// <summary>
    /// Factory method to create a system default category.
    /// System categories are immutable and global (user_id = NULL in database).
    /// Feature 3: No userId parameter required.
    /// </summary>
    public static Category CreateSystemDefault(
        CategoryId id,
        CategoryName name,
        CategoryType type,
        ColorHex color,
        string iconName)
    {
        return new Category(
            id,
            userId: null,
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

    /// <summary>
    /// Feature 3: Helper property to determine if this is a global system category (null UserId).
    /// </summary>
    public bool IsGlobal => UserId is null;

    /// <summary>
    /// Feature 3: Helper property to determine if this is user-scoped (non-null UserId).
    /// </summary>
    public bool IsUserScoped => UserId is not null;

    /// <summary>
    /// Feature 3: Check if this category is owned by a specific user.
    /// Returns false for system categories (null UserId).
    /// </summary>
    public bool IsOwnedByUser(UserId userId)
    {
        return UserId != null && UserId.Value == userId.Value;
    }

    /// <summary>
    /// Feature 3: Check if this category is accessible to a specific user.
    /// System categories (IsSystemDefault=true) are accessible to all users.
    /// User-scoped categories are only accessible to their owner.
    /// </summary>
    public bool IsAccessibleToUser(UserId userId)
    {
        return IsSystemDefault || IsOwnedByUser(userId);
    }
}

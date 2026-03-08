namespace SauronSheet.Domain.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exceptions;
using Entities;
using Repositories;
using ValueObjects;

/// <summary>
/// Domain service for cross-entity category logic.
/// Handles validation, business rules, and system default category seeding.
/// </summary>
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    /// <summary>
    /// Validate that a category name is unique for a user and not a system default.
    /// Throws DomainException if validation fails.
    /// </summary>
    public async Task ValidateUniqueName(UserId userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.");
        }

        var trimmedName = name.Trim();

        // Check for duplicates
        var existing = await _categoryRepository.FindByNameAndUserAsync(userId, trimmedName);
        if (existing != null)
        {
            throw new DomainException($"A category with name '{trimmedName}' already exists for this user.");
        }

        // Check against system default names
        var systemDefaults = GetSystemDefaults(userId);
        if (systemDefaults.Any(c => c.Name.Value.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"The name '{trimmedName}' is reserved for a system category and cannot be used.");
        }
    }

    /// <summary>
    /// Check if a category can be deleted.
    /// Returns false if system default or has active transactions.
    /// </summary>
    public bool CanDeleteCategory(Category category, bool hasActiveTransactions)
    {
        if (category == null)
        {
            throw new ArgumentNullException(nameof(category));
        }

        return category.CanDelete(hasActiveTransactions);
    }

    /// <summary>
    /// Get immutable list of 24 system default categories.
    /// Organized into 6 groups: Income (5), Fixed Expenses (5), Variable Expenses (5),
    /// Lifestyle (5), and Finance & Other (4).
    /// </summary>
    public IReadOnlyList<Category> GetSystemDefaults(UserId userId)
    {
        var categories = new List<Category>();

        // Income (5)
        categories.AddRange(new[]
        {
            CreateDefault(userId, "Salary", CategoryType.Income, "#27AE60", "building-dollar"),
            CreateDefault(userId, "Sales", CategoryType.Income, "#27AE60", "shopping-bag"),
            CreateDefault(userId, "Investments", CategoryType.Income, "#27AE60", "trending-up"),
            CreateDefault(userId, "Gifts", CategoryType.Income, "#27AE60", "gift"),
            CreateDefault(userId, "Other Income", CategoryType.Income, "#27AE60", "coins")
        });

        // Fixed Expenses (5)
        categories.AddRange(new[]
        {
            CreateDefault(userId, "Housing", CategoryType.Expense, "#E74C3C", "house"),
            CreateDefault(userId, "Utilities", CategoryType.Expense, "#E74C3C", "lightning-charge"),
            CreateDefault(userId, "Insurance", CategoryType.Expense, "#E74C3C", "shield-check"),
            CreateDefault(userId, "Subscriptions", CategoryType.Expense, "#E74C3C", "bookmark"),
            CreateDefault(userId, "Education", CategoryType.Expense, "#E74C3C", "book")
        });

        // Variable Expenses (5)
        categories.AddRange(new[]
        {
            CreateDefault(userId, "Groceries", CategoryType.Expense, "#F39C12", "basket"),
            CreateDefault(userId, "Transportation", CategoryType.Expense, "#F39C12", "car-front"),
            CreateDefault(userId, "Personal Care", CategoryType.Expense, "#F39C12", "person-check"),
            CreateDefault(userId, "Home", CategoryType.Expense, "#F39C12", "hammer"),
            CreateDefault(userId, "Pets", CategoryType.Expense, "#F39C12", "paw")
        });

        // Lifestyle (5)
        categories.AddRange(new[]
        {
            CreateDefault(userId, "Restaurants", CategoryType.Expense, "#9B59B6", "cup-straw"),
            CreateDefault(userId, "Entertainment", CategoryType.Expense, "#9B59B6", "film"),
            CreateDefault(userId, "Shopping", CategoryType.Expense, "#9B59B6", "bag-check"),
            CreateDefault(userId, "Travel", CategoryType.Expense, "#9B59B6", "airplane"),
            CreateDefault(userId, "Health & Wellness", CategoryType.Expense, "#9B59B6", "heart-pulse")
        });

        // Finance & Other (4)
        categories.AddRange(new[]
        {
            CreateDefault(userId, "Debt Payments", CategoryType.Expense, "#3498DB", "credit-card-2-back"),
            CreateDefault(userId, "Savings & Investment", CategoryType.Expense, "#3498DB", "piggy-bank"),
            CreateDefault(userId, "Donations", CategoryType.Expense, "#3498DB", "hand-thumbs-up"),
            CreateDefault(userId, "Unexpected Expenses", CategoryType.Expense, "#3498DB", "exclamation-triangle")
        });

        return categories.AsReadOnly();
    }

    /// <summary>
    /// Helper method to create a system default category with generated ID.
    /// </summary>
    private static Category CreateDefault(
        UserId userId,
        string name,
        CategoryType type,
        string color,
        string icon)
    {
        return Category.CreateSystemDefault(
            CategoryId.New(),
            userId,
            CategoryName.Create(name),
            type,
            ColorHex.Create(color),
            icon);
    }
}

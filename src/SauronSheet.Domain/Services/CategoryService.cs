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
/// Feature 3: System categories now cached as lazy singleton (persisted in database with NULL user_id).
/// </summary>
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    // Feature 3: Lazy singleton cache for system defaults
    private static IReadOnlyList<Category>? _cachedSystemDefaults;
    private static readonly object _cacheLock = new();

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
        var systemDefaults = GetSystemDefaults();
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
    /// Get immutable list of 24 system default categories (cached after first call).
    /// Feature 3: Returns hardcoded reference with NULL user_id.
    /// Organized into 6 groups: Income (5), Fixed Expenses (5), Variable Expenses (5),
    /// Lifestyle (5), and Finance & Other (4).
    /// </summary>
    public IReadOnlyList<Category> GetSystemDefaults()
    {
        // Feature 3: Lazy singleton pattern with double-check locking
        if (_cachedSystemDefaults != null)
        {
            return _cachedSystemDefaults;
        }

        lock (_cacheLock)
        {
            if (_cachedSystemDefaults != null)
            {
                return _cachedSystemDefaults;
            }

            var categories = new List<Category>();

            // Income (5)
            categories.AddRange(new[]
            {
                CreateDefault("Salary", CategoryType.Income, "#27AE60", "building-dollar"),
                CreateDefault("Sales", CategoryType.Income, "#27AE60", "shopping-bag"),
                CreateDefault("Investments", CategoryType.Income, "#27AE60", "trending-up"),
                CreateDefault("Gifts", CategoryType.Income, "#27AE60", "gift"),
                CreateDefault("Other Income", CategoryType.Income, "#27AE60", "coins")
            });

            // Fixed Expenses (5)
            categories.AddRange(new[]
            {
                CreateDefault("Housing", CategoryType.Expense, "#E74C3C", "house"),
                CreateDefault("Utilities", CategoryType.Expense, "#E74C3C", "lightbulb"),
                CreateDefault("Insurance", CategoryType.Expense, "#E74C3C", "shield"),
                CreateDefault("Subscription", CategoryType.Expense, "#E74C3C", "bell"),
                CreateDefault("Education", CategoryType.Expense, "#E74C3C", "book")
            });

            // Variable Expenses (5)
            categories.AddRange(new[]
            {
                CreateDefault("Groceries", CategoryType.Expense, "#F39C12", "shopping-cart"),
                CreateDefault("Transportation", CategoryType.Expense, "#F39C12", "car"),
                CreateDefault("Entertainment", CategoryType.Expense, "#F39C12", "popcorn"),
                CreateDefault("Dining Out", CategoryType.Expense, "#F39C12", "utensils"),
                CreateDefault("Shopping", CategoryType.Expense, "#F39C12", "shopping-bag")
            });

            // Lifestyle (5)
            categories.AddRange(new[]
            {
                CreateDefault("Coffee", CategoryType.Expense, "#9B59B6", "coffee"),
                CreateDefault("Fitness", CategoryType.Expense, "#9B59B6", "dumbbell"),
                CreateDefault("Healthcare", CategoryType.Expense, "#9B59B6", "heart"),
                CreateDefault("Hobbies", CategoryType.Expense, "#9B59B6", "palette"),
                CreateDefault("Gifts Given", CategoryType.Expense, "#9B59B6", "gift")
            });

            // Finance & Other (4)
            categories.AddRange(new[]
            {
                CreateDefault("Phone", CategoryType.Expense, "#3498DB", "phone"),
                CreateDefault("Internet", CategoryType.Expense, "#3498DB", "wifi"),
                CreateDefault("Gas", CategoryType.Expense, "#3498DB", "gas-pump"),
                CreateDefault("Other Expense", CategoryType.Expense, "#3498DB", "dots-horizontal")
            });

            _cachedSystemDefaults = categories.AsReadOnly();
            return _cachedSystemDefaults;
        }
    }

    /// <summary>
    /// Helper method to create a system default category with generated ID.
    /// Feature 3: No userId parameter - system categories have NULL user_id in database.
    /// </summary>
    private static Category CreateDefault(
        string name,
        CategoryType type,
        string color,
        string icon)
    {
        return Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create(name),
            type,
            ColorHex.Create(color),
            icon);
    }
}

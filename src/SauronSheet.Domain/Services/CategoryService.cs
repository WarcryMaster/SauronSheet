namespace SauronSheet.Domain.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using Repositories;
using ValueObjects;

/// <summary>
/// Domain service for cross-entity category logic.
/// </summary>
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    /// <summary>
    /// Validate that a category name is unique for a user.
    /// </summary>
    public async Task ValidateUniqueName(UserId userId, string name)
    {
        var existing = await _categoryRepository.FindByNameAndUserAsync(userId, name);
        if (existing != null)
            throw new InvalidOperationException($"Category '{name}' already exists for this user.");
    }

    /// <summary>
    /// Check if a category can be deleted.
    /// </summary>
    public bool CanDeleteCategory(Category category, bool hasActiveTransactions)
    {
        return category.CanDelete(hasActiveTransactions);
    }

    /// <summary>
    /// Get the 4 system default categories.
    /// </summary>
    public List<Category> GetSystemDefaults(UserId userId)
    {
        return new List<Category>
        {
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Groceries"),
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Transport"),
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Utilities"),
            Category.CreateSystemDefault(new CategoryId(Guid.NewGuid()), userId, "Other")
        };
    }
}

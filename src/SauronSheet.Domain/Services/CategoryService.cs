namespace SauronSheet.Domain.Services;

using System;
using System.Threading.Tasks;
using Exceptions;
using Entities;
using Repositories;
using ValueObjects;

/// <summary>
/// Domain service for cross-entity category logic.
/// Handles validation and business rules.
/// System defaults have been removed — only user-created categories participate.
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
    /// Throws DomainException if validation fails.
    /// System default name check removed — defaults no longer exist.
    /// </summary>
    public async Task ValidateUniqueName(UserId userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.");
        }

        string trimmedName = name.Trim();

        // Check for duplicates within user's categories
        Category? existing = await _categoryRepository.FindByNameAndUserAsync(userId, trimmedName);
        if (existing != null)
        {
            throw new DomainException($"A category with name '{trimmedName}' already exists for this user.");
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
}

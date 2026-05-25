namespace SauronSheet.Domain.Repositories;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;

/// <summary>
/// Repository interface for Category aggregate.
/// Defined in Domain layer as contract; implemented in Infrastructure.
/// System defaults have been removed — only user-created categories remain.
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id);
    Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId);
    Task<Category?> FindByNameAndUserAsync(UserId userId, string name);
    Task<Category?> FindByNameAsync(string name);

    /// <summary>
    /// Find a user-scoped category by its normalized deduplication key.
    /// Used by PDF import resolver to detect existing categories before creating new ones.
    /// normalizedName must be pre-computed via CategoryNormalizer.Normalize().
    /// </summary>
    Task<Category?> FindByNormalizedNameAndUserAsync(UserId userId, string normalizedName);

    /// <summary>
    /// Insert a new category and its pre-computed normalized name.
    /// normalizedName is mandatory — the DB column is NOT NULL after migration 011.
    /// Caller must use CategoryNormalizer.Normalize(category.Name.Value).
    /// </summary>
    Task AddAsync(Category category, string normalizedName);

    Task UpdateAsync(Category category);
    Task DeleteAsync(CategoryId id);
    Task<bool> HasTransactionsAsync(CategoryId categoryId);
}

namespace SauronSheet.Domain.Repositories;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;

/// <summary>
/// Repository interface for Subcategory aggregate.
/// Defined in Domain layer as contract; implemented in Infrastructure.
/// </summary>
public interface ISubcategoryRepository
{
    Task<Subcategory?> GetByIdAsync(SubcategoryId id);
    Task<IReadOnlyList<Subcategory>> GetByUserIdAsync(UserId userId);
    Task<IReadOnlyList<Subcategory>> GetByCategoryIdAsync(CategoryId categoryId);
    Task<Subcategory?> FindByNameAsync(UserId userId, CategoryId categoryId, string name);

    /// <summary>
    /// Find a subcategory by its normalized deduplication key within a category scope.
    /// Used by PDF import resolver. normalizedName via CategoryNormalizer.Normalize().
    /// Scoped to (userId, categoryId) — not global.
    /// </summary>
    Task<Subcategory?> FindByNormalizedNameAsync(UserId userId, CategoryId categoryId, string normalizedName);

    /// <summary>
    /// Insert a new subcategory and its pre-computed normalized name.
    /// normalizedName is mandatory — the DB column is NOT NULL after migration 011.
    /// Caller must use CategoryNormalizer.Normalize(subcategory.Name.Value).
    /// </summary>
    Task AddAsync(Subcategory subcategory, string normalizedName);

    Task UpdateAsync(Subcategory subcategory, string normalizedName);
    Task DeleteAsync(SubcategoryId id);
    Task<bool> HasTransactionsAsync(SubcategoryId subcategoryId);
}

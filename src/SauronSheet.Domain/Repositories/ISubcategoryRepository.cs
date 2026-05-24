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
    Task AddAsync(Subcategory subcategory);
}

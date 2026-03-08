namespace SauronSheet.Domain.Repositories;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;

/// <summary>
/// Repository interface for Category aggregate.
/// Defined in Domain layer as contract; implemented in Infrastructure.
/// Feature 3: GetSystemDefaultsAsync no longer requires userId parameter (system categories shared globally).
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id);
    Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId);
    Task<Category?> FindByNameAndUserAsync(UserId userId, string name);
    Task<Category?> FindByNameAsync(string name); // Feature 3: New method for global name search
    Task<IReadOnlyList<Category>> GetSystemDefaultsAsync(); // Feature 3: No userId parameter
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(CategoryId id);
    Task<bool> HasTransactionsAsync(CategoryId categoryId);
}

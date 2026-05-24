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
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(CategoryId id);
    Task<bool> HasTransactionsAsync(CategoryId categoryId);
}

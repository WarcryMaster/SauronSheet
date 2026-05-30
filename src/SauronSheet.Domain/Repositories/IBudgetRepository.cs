namespace SauronSheet.Domain.Repositories;

using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;

/// <summary>
/// Repository interface for Budget aggregate.
/// Defined in Domain layer as contract; implemented in Infrastructure.
/// </summary>
public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(BudgetId id);
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId);
    Task<IReadOnlyList<Budget>> GetByUserAndCategoryAsync(UserId userId, CategoryId categoryId);
    Task<Budget?> GetActiveByUserAndCategoryAsync(UserId userId, CategoryId categoryId, DateOnly asOf);
    Task<IReadOnlyList<Budget>> GetByUserAndDateRangeAsync(UserId userId, DateOnly from, DateOnly? to);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(BudgetId id);
}

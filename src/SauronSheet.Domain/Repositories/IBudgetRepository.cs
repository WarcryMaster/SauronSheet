namespace SauronSheet.Domain.Repositories;

using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;

/// <summary>
/// Repository interface for Budget aggregate.
/// Defined in Domain layer as contract; implemented in Infrastructure.
/// Phase 5: Budget Management.
/// </summary>
public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(BudgetId id);
    Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId);
    Task<Budget?> GetByUserAndCategoryAndMonthAsync(UserId userId, CategoryId categoryId, DateRange period);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(BudgetId id);
}

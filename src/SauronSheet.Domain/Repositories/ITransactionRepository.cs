namespace SauronSheet.Domain.Repositories;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;
using Specifications;

/// <summary>
/// Repository interface for Transaction aggregate.
/// Defined in Domain layer as contract; implemented in Infrastructure.
/// </summary>
public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(TransactionId id);
    Task<IReadOnlyList<Transaction>> GetByUserIdAsync(UserId userId);
    Task<IReadOnlyList<Transaction>> FindBySpecificationAsync(ISpecification<Transaction> specification);
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(TransactionId id);
    Task<bool> ExistsAsync(TransactionId id);
    Task<bool> ExistsDuplicateAsync(UserId userId, DateTime date, decimal amount, string description);

    /// <summary>
    /// Gets transaction counts grouped by category.
    /// CRITICAL FIX I-4: Added to support CategoryDto.TransactionCount calculation.
    /// </summary>
    /// <param name="categoryIds">List of category IDs to count transactions for</param>
    /// <returns>Dictionary mapping CategoryId to transaction count</returns>
    Task<Dictionary<CategoryId, int>> GetCountsByCategoriesAsync(List<CategoryId> categoryIds);
}

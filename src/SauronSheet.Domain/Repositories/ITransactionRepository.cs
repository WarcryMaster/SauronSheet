namespace SauronSheet.Domain.Repositories;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Specifications;

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
    /// Gets the transaction date range for a user.
    /// Returns null when the user has no transactions.
    /// </summary>
    Task<(DateTime MinDate, DateTime MaxDate)?> GetDateRangeAsync(UserId userId);

    /// <summary>
    /// Deletes multiple transactions atomically for a user.
    /// Feature 004: Bulk delete implementation.
    /// If any transaction fails to delete, all deletions are rolled back (atomic semantics).
    /// </summary>
    /// <param name="userId">User owner of the transactions (multi-tenant isolation)</param>
    /// <param name="transactionIds">IDs of transactions to delete</param>
    /// <returns>Number of transactions successfully deleted</returns>
    Task<int> DeleteTransactionsByIdsAsync(UserId userId, IEnumerable<TransactionId> transactionIds);

    /// <summary>
    /// Gets transaction counts grouped by category.
    /// CRITICAL FIX I-4: Added to support CategoryDto.TransactionCount calculation.
    /// </summary>
    /// <param name="categoryIds">List of category IDs to count transactions for</param>
    /// <returns>Dictionary mapping CategoryId to transaction count</returns>
    Task<Dictionary<CategoryId, int>> GetCountsByCategoriesAsync(List<CategoryId> categoryIds);
}

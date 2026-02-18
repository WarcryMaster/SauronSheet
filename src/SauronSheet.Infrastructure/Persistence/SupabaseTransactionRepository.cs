namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Specifications;

/// <summary>
/// Supabase implementation of ITransactionRepository.
/// CRITICAL FIX C-1: Supabase.Client injected via DI.
/// </summary>
public class SupabaseTransactionRepository : ITransactionRepository
{
    private readonly Supabase.Client _client;

    public SupabaseTransactionRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<Transaction?> GetByIdAsync(TransactionId id)
    {
        // TODO Phase 3F: Implement Supabase query
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase GetByIdAsync");
    }

    public async Task<IReadOnlyList<Transaction>> GetByUserIdAsync(UserId userId)
    {
        // TODO Phase 3F: Implement Supabase query
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase GetByUserIdAsync");
    }

    public async Task<IReadOnlyList<Transaction>> FindBySpecificationAsync(
        ISpecification<Transaction> specification)
    {
        // TODO Phase 3F: Implement specification to Postgrest query translation
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase FindBySpecificationAsync");
    }

    public async Task AddAsync(Transaction transaction)
    {
        // TODO Phase 3F: Implement Supabase insert
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase AddAsync");
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        // TODO Phase 3F: Implement Supabase update
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase UpdateAsync");
    }

    public async Task DeleteAsync(TransactionId id)
    {
        // TODO Phase 3F: Implement Supabase delete
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase DeleteAsync");
    }

    public async Task<bool> ExistsAsync(TransactionId id)
    {
        // TODO Phase 3F: Implement Supabase exists check
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase ExistsAsync");
    }

    /// <summary>
    /// Checks if a duplicate transaction exists.
    /// CRITICAL FIX C-3: Duplicate detection ignores currency.
    /// Rationale: Users are unlikely to have same-day, same-amount transactions
    /// in different currencies. If this becomes an issue, currency can be added post-MVP.
    /// </summary>
    public async Task<bool> ExistsDuplicateAsync(
        UserId userId, DateTime date, decimal amount, string description)
    {
        // TODO Phase 3F: Implement duplicate check
        // NOTE: Currency is NOT checked
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase ExistsDuplicateAsync");
    }

    /// <summary>
    /// CRITICAL FIX I-4: Get transaction counts grouped by category.
    /// </summary>
    public async Task<Dictionary<CategoryId, int>> GetCountsByCategoriesAsync(List<CategoryId> categoryIds)
    {
        // TODO Phase 3F: Implement batch count query
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase GetCountsByCategoriesAsync");
    }
}

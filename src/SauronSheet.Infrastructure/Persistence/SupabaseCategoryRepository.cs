namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;

/// <summary>
/// Supabase implementation of ICategoryRepository.
/// </summary>
public class SupabaseCategoryRepository : ICategoryRepository
{
    private readonly Supabase.Client _client;

    public SupabaseCategoryRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<Category?> GetByIdAsync(CategoryId id)
    {
        // TODO Phase 3F: Implement Supabase query
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase GetByIdAsync");
    }

    public async Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId)
    {
        // TODO Phase 3F: Implement Supabase query
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase GetByUserIdAsync");
    }

    public async Task<Category?> FindByNameAndUserAsync(UserId userId, string name)
    {
        // TODO Phase 3F: Implement Supabase query
        // SELECT * FROM categories WHERE user_id = userId.Value AND name = name LIMIT 1
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase FindByNameAndUserAsync");
    }

    public async Task<IReadOnlyList<Category>> GetSystemDefaultsAsync(UserId userId)
    {
        // TODO Phase 3F: Implement Supabase query
        // SELECT * FROM categories WHERE user_id = userId.Value AND is_system_default = TRUE
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase GetSystemDefaultsAsync");
    }

    public async Task AddAsync(Category category)
    {
        // TODO Phase 3F: Implement Supabase insert
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase AddAsync");
    }

    public async Task UpdateAsync(Category category)
    {
        // TODO Phase 3F: Implement Supabase update
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase UpdateAsync");
    }

    public async Task DeleteAsync(CategoryId id)
    {
        // TODO Phase 3F: Implement Supabase delete
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase DeleteAsync");
    }

    public async Task<bool> HasTransactionsAsync(CategoryId categoryId)
    {
        // TODO Phase 3F: Implement Supabase query
        // SELECT COUNT(*) FROM transactions WHERE category_id = categoryId.Value
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase HasTransactionsAsync");
    }
}

namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;
using Mapping;
using Postgrest;

/// <summary>
/// Supabase implementation of ISubcategoryRepository.
/// Uses Postgrest client for CRUD operations on the subcategories table.
/// </summary>
public class SupabaseSubcategoryRepository : ISubcategoryRepository
{
    private readonly Supabase.Client _client;

    public SupabaseSubcategoryRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<Subcategory?> GetByIdAsync(SubcategoryId id)
    {
        var idString = id.Value.ToString();
        var response = await _client.From<SubcategoryRow>()
            .Where(x => x.Id == idString)
            .Get();

        var row = response.Models.FirstOrDefault();
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Subcategory>> GetByUserIdAsync(UserId userId)
    {
        var userIdString = userId.Value;
        var response = await _client.From<SubcategoryRow>()
            .Where(x => x.UserId == userIdString)
            .Get();

        return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<Subcategory>> GetByCategoryIdAsync(CategoryId categoryId)
    {
        var categoryIdString = categoryId.Value.ToString();
        var response = await _client.From<SubcategoryRow>()
            .Where(x => x.CategoryId == categoryIdString)
            .Get();

        return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<Subcategory?> FindByNameAsync(UserId userId, CategoryId categoryId, string name)
    {
        var userIdString = userId.Value;
        var categoryIdString = categoryId.Value.ToString();
        var response = await _client.From<SubcategoryRow>()
            .Where(x => x.UserId == userIdString)
            .Where(x => x.CategoryId == categoryIdString)
            .Get();

        // Case-insensitive comparison in memory
        // Postgrest does not support OR conditions, and case-insensitive LIKE
        // depends on column collation. In-memory filtering is safer.
        return response.Models
            .Select(r => r.ToDomain())
            .FirstOrDefault(s => s.Name.Value.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Subcategory?> FindByNormalizedNameAsync(UserId userId, CategoryId categoryId, string normalizedName)
    {
        var userIdString = userId.Value;
        var categoryIdString = categoryId.Value.ToString();
        var response = await _client.From<SubcategoryRow>()
            .Where(x => x.UserId == userIdString)
            .Where(x => x.CategoryId == categoryIdString)
            .Where(x => x.NormalizedName == normalizedName)
            .Limit(1)
            .Get();

        var row = response.Models.FirstOrDefault();
        return row?.ToDomain();
    }

    public async Task AddAsync(Subcategory subcategory, string normalizedName)
    {
        try
        {
            var row = MappingExtensions.FromDomain(subcategory, normalizedName);
            await _client.From<SubcategoryRow>().Insert(row);
        }
        catch (Postgrest.Exceptions.PostgrestException ex)
            when (ex.Content?.Contains("23505", StringComparison.Ordinal) == true)
        {
            // Translate UNIQUE constraint violation into a domain exception
            // so the Application layer can perform a retry-get without referencing Postgrest directly.
            Sentry.SentrySdk.AddBreadcrumb(
                $"23505 duplicate on subcategory insert (normalized: {normalizedName})",
                "repo.subcategory",
                data: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["normalizedName"] = normalizedName,
                    ["subcategoryId"]  = subcategory.Id.Value.ToString()
                });
            throw new DuplicateEntityException("subcategory", normalizedName);
        }
    }

    public async Task UpdateAsync(Subcategory subcategory, string normalizedName)
    {
        var idString = subcategory.Id.Value.ToString();
        var row = MappingExtensions.FromDomain(subcategory, normalizedName);
        await _client.From<SubcategoryRow>()
            .Where(x => x.Id == idString)
            .Update(row);
    }

    public async Task DeleteAsync(SubcategoryId id)
    {
        var idString = id.Value.ToString();
        await _client.From<SubcategoryRow>()
            .Where(x => x.Id == idString)
            .Delete();
    }

}

namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
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
        try
        {
            var idString = id.Value.ToString();
            var response = await _client.From<SubcategoryRow>()
                .Where(x => x.Id == idString)
                .Get();

            var row = response.Models.FirstOrDefault();
            return row?.ToDomain();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("repo", "SupabaseSubcategoryRepository.GetByIdAsync");
                scope.SetTag("subcategoryId", id.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<IReadOnlyList<Subcategory>> GetByUserIdAsync(UserId userId)
    {
        try
        {
            var userIdString = userId.Value;
            var response = await _client.From<SubcategoryRow>()
                .Where(x => x.UserId == userIdString)
                .Get();

            return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("repo", "SupabaseSubcategoryRepository.GetByUserIdAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<IReadOnlyList<Subcategory>> GetByCategoryIdAsync(CategoryId categoryId)
    {
        try
        {
            var categoryIdString = categoryId.Value.ToString();
            var response = await _client.From<SubcategoryRow>()
                .Where(x => x.CategoryId == categoryIdString)
                .Get();

            return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("repo", "SupabaseSubcategoryRepository.GetByCategoryIdAsync");
                scope.SetTag("categoryId", categoryId.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<Subcategory?> FindByNameAsync(UserId userId, CategoryId categoryId, string name)
    {
        try
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
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("repo", "SupabaseSubcategoryRepository.FindByNameAsync");
                scope.SetTag("userId", userId.Value);
                scope.SetTag("categoryId", categoryId.Value.ToString());
                scope.SetTag("name", name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task AddAsync(Subcategory subcategory)
    {
        try
        {
            var row = MappingExtensions.FromDomain(subcategory);
            await _client.From<SubcategoryRow>().Insert(row);
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("repo", "SupabaseSubcategoryRepository.AddAsync");
                scope.SetTag("subcategoryId", subcategory.Id.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }
}

namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;

/// <summary>
/// Postgrest DTO for the categories table.
/// </summary>
[Table("categories")]
internal class CategoryRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id")]
    public string? UserId { get; set; } = null;

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("type")]
    public string Type { get; set; } = "Expense";

    [Column("color")]
    public string Color { get; set; } = "#3498DB";

    [Column("icon_name")]
    public string IconName { get; set; } = "tag";

    [Column("is_system_default")]
    public bool IsSystemDefault { get; set; }

    /// <summary>
    /// Normalized deduplication key for this category name.
    /// Computed by CategoryNormalizer.Normalize(Name); stored via migration 011.
    /// NOT NULL in DB after migration 011.
    /// </summary>
    [Column("normalized_name")]
    public string? NormalizedName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Feature 3: Updated to handle nullable UserId from database.
    /// </summary>
    public Category ToDomain()
    {
        var categoryType = Type == "Income" ? CategoryType.Income : CategoryType.Expense;

        // Feature 3: Handle nullable UserId from database
        UserId? userId = string.IsNullOrEmpty(UserId) 
            ? null 
            : new UserId(UserId);

        if (IsSystemDefault)
        {
            return Category.CreateSystemDefault(
                new CategoryId(Guid.Parse(Id)),
                CategoryName.Create(Name),
                categoryType,
                ColorHex.Create(Color),
                IconName);
        }

        return new Category(
            new CategoryId(Guid.Parse(Id)),
            userId ?? throw new InvalidOperationException($"User category {Id} missing user_id"),
            CategoryName.Create(Name),
            categoryType,
            ColorHex.Create(Color),
            IconName);
    }

    /// <summary>
    /// Feature 3: Updated to handle nullable UserId.
    /// </summary>
    public static CategoryRow FromDomain(Category c)
    {
        return new CategoryRow
        {
            Id = c.Id.Value.ToString(),
            UserId = c.UserId?.Value,
            Name = c.Name.Value,
            Type = c.Type.ToString(),
            Color = c.Color.Value,
            IconName = c.IconName,
            IsSystemDefault = c.IsSystemDefault,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }

    /// <summary>
    /// Converts category to insert-safe DTO with normalized name.
    /// Timestamps are assigned by database triggers, not by client.
    /// normalizedName is pre-computed by the caller (CategoryNormalizer.Normalize).
    /// </summary>
    public static CategoryRow FromDomainForInsert(Category c, string normalizedName)
    {
        return new CategoryRow
        {
            Id = c.Id.Value.ToString(),
            UserId = c.UserId?.Value,
            Name = c.Name.Value,
            NormalizedName = normalizedName,
            Type = c.Type.ToString(),
            Color = c.Color.Value,
            IconName = c.IconName,
            IsSystemDefault = c.IsSystemDefault
            // NOTE: Do NOT set CreatedAt or UpdatedAt - let database triggers handle timestamps
        };
    }
}

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
        var idString = id.Value.ToString();
        var response = await _client.From<CategoryRow>()
            .Where(x => x.Id == idString)
            .Get();

        var row = response.Models.FirstOrDefault();
        return row?.ToDomain();
    }

    /// <summary>
    /// Get user-scoped categories only (system defaults removed in Chunk 3).
    /// System default categories still exist in the database for backward
    /// compatibility with existing transactions, but are excluded from the
    /// category management UI.
    /// </summary>
    public async Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId)
    {
        try
        {
            var response = await _client.From<CategoryRow>()
                .Where(x => x.UserId == userId.Value)
                .Get();

            // Order BEFORE converting to domain — CategoryRow.Name is a string (IComparable),
            // while Category.Name is a CategoryName value object that does not implement IComparable.
            return response.Models
                .OrderBy(r => r.Name)
                .Select(r => r.ToDomain())
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseCategoryRepository.GetByUserIdAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<Category?> FindByNameAndUserAsync(UserId userId, string name)
    {
        try
        {
            var response = await _client.From<CategoryRow>()
                .Where(x => x.UserId == userId.Value)
                .Where(x => x.Name == name)
                .Limit(1)
                .Get();

            var row = response.Models.FirstOrDefault();
            return row?.ToDomain();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseCategoryRepository.FindByNameAndUserAsync");
                scope.SetTag("userId", userId.Value);
                scope.SetTag("name", name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    /// <summary>
    /// Feature 3: Find category by name across all scopes (user + system).
    /// Used for validation in CreateCategoryCommand.
    /// </summary>
    public async Task<Category?> FindByNameAsync(string name)
    {
        try
        {
            var response = await _client.From<CategoryRow>()
                .Where(x => x.Name == name)  // NO user_id filter - global search
                .Limit(1)
                .Get();

            return response.Models.FirstOrDefault()?.ToDomain();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseCategoryRepository.FindByNameAsync");
                scope.SetTag("name", name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    /// <summary>
    /// Feature 3: Get system default categories (NULL user_id).
    /// No userId parameter required - system categories are global.
    /// </summary>
    public async Task<IReadOnlyList<Category>> GetSystemDefaultsAsync()
    {
        try
        {
            var response = await _client.From<CategoryRow>()
                .Where(x => x.UserId == null)
                .Where(x => x.IsSystemDefault == true)
                .Get();

            return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseCategoryRepository.GetSystemDefaultsAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<Category?> FindByNormalizedNameAndUserAsync(UserId userId, string normalizedName)
    {
        try
        {
            var userIdString = userId.Value;
            var response = await _client.From<CategoryRow>()
                .Where(x => x.UserId == userIdString)
                .Where(x => x.NormalizedName == normalizedName)
                .Limit(1)
                .Get();

            var row = response.Models.FirstOrDefault();
            return row?.ToDomain();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseCategoryRepository.FindByNormalizedNameAndUserAsync");
                scope.SetTag("userId", userId.Value);
                scope.SetTag("normalizedName", normalizedName);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task AddAsync(Category category, string normalizedName)
    {
        try
        {
            var row = CategoryRow.FromDomainForInsert(category, normalizedName);
            await _client.From<CategoryRow>().Insert(row);
        }
        catch (Postgrest.Exceptions.PostgrestException ex)
            when (ex.Content?.Contains("23505", StringComparison.Ordinal) == true)
        {
            // Translate UNIQUE constraint violation into a domain exception
            // so the Application layer can perform a retry-get without referencing Postgrest directly.
            Sentry.SentrySdk.AddBreadcrumb(
                $"23505 duplicate on category insert (normalized: {normalizedName})",
                "repo.category",
                data: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["normalizedName"] = normalizedName,
                    ["categoryId"]     = category.Id.Value.ToString()
                });
            throw new DuplicateEntityException("category", normalizedName);
        }
    }

    public async Task UpdateAsync(Category category)
    {
        var idString = category.Id.Value.ToString();
        await _client.From<CategoryRow>()
            .Where(x => x.Id == idString)
            .Update(CategoryRow.FromDomain(category));
    }

    public async Task DeleteAsync(CategoryId id)
    {
        var idString = id.Value.ToString();
        await _client.From<CategoryRow>()
            .Where(x => x.Id == idString)
            .Delete();
    }

    public async Task<bool> HasTransactionsAsync(CategoryId categoryId)
    {
        var catIdStr = categoryId.Value.ToString();
        var response = await _client.From<TransactionRow>()
            .Where(x => x.CategoryId == catIdStr)
            .Limit(1)
            .Get();

        return response.Models.Any();
    }

    public async Task<int> GetTransactionCountAsync(CategoryId categoryId)
    {
        var catIdStr = categoryId.Value.ToString();
        var response = await _client.From<TransactionRow>()
            .Where(x => x.CategoryId == catIdStr)
            .Get();

        return response.Models.Count;
    }
}

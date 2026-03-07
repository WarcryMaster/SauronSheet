namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;
using Domain.Entities;
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
    public string UserId { get; set; } = "";

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("color")]
    public string? Color { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("is_system_default")]
    public bool IsSystemDefault { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public Category ToDomain()
    {
        if (IsSystemDefault)
        {
            return Category.CreateSystemDefault(
                new CategoryId(Guid.Parse(Id)),
                new UserId(UserId),
                Name);
        }

        return new Category(
            new CategoryId(Guid.Parse(Id)),
            new UserId(UserId),
            Name,
            Color,
            Icon);
    }

    public static CategoryRow FromDomain(Category c)
    {
        return new CategoryRow
        {
            Id = c.Id.Value.ToString(),
            UserId = c.UserId.Value,
            Name = c.Name,
            Color = c.Color,
            Icon = c.Icon,
            IsSystemDefault = c.IsSystemDefault,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }

    /// <summary>
    /// Converts category to insert-safe DTO (excludes server-managed timestamps).
    /// Timestamps are assigned by database triggers, not by client.
    /// </summary>
    public static CategoryRow FromDomainForInsert(Category c)
    {
        return new CategoryRow
        {
            Id = c.Id.Value.ToString(),
            UserId = c.UserId.Value,
            Name = c.Name,
            Color = c.Color,
            Icon = c.Icon,
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

    public async Task<IReadOnlyList<Category>> GetByUserIdAsync(UserId userId)
    {
        var response = await _client.From<CategoryRow>()
            .Where(x => x.UserId == userId.Value)
            .Order("name", Constants.Ordering.Ascending)
            .Get();

        return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<Category?> FindByNameAndUserAsync(UserId userId, string name)
    {
        var response = await _client.From<CategoryRow>()
            .Where(x => x.UserId == userId.Value)
            .Where(x => x.Name == name)
            .Limit(1)
            .Get();

        var row = response.Models.FirstOrDefault();
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Category>> GetSystemDefaultsAsync(UserId userId)
    {
        var response = await _client.From<CategoryRow>()
            .Where(x => x.UserId == userId.Value)
            .Where(x => x.IsSystemDefault == true)
            .Get();

        return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public async Task AddAsync(Category category)
    {
        var row = CategoryRow.FromDomainForInsert(category);
        await _client.From<CategoryRow>().Insert(row);
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
}

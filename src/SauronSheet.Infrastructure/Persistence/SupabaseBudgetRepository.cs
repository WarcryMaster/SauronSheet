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
/// Postgrest DTO for the redesigned budgets table (permanent policy model).
/// Columns: effective_from (DATE), effective_until (DATE?),
/// period_granularity (VARCHAR).
/// </summary>
[Table("budgets")]
internal class BudgetRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id")]
    public string UserId { get; set; } = "";

    [Column("category_id")]
    public string CategoryId { get; set; } = "";

    [Column("effective_from")]
    public DateTime EffectiveFrom { get; set; }

    [Column("effective_until")]
    public DateTime? EffectiveUntil { get; set; }

    [Column("period_granularity")]
    public string PeriodGranularity { get; set; } = "Monthly";

    [Column("limit_amount")]
    public decimal LimitAmount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "EUR";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Maps this infrastructure row to a domain Budget entity.
    /// Uses DateOnly.FromDateTime to strip time components from DATE columns.
    /// </summary>
    public Budget ToDomain()
    {
        var effectiveFrom = DateOnly.FromDateTime(EffectiveFrom);
        DateOnly? effectiveUntil = EffectiveUntil.HasValue
            ? DateOnly.FromDateTime(EffectiveUntil.Value)
            : null;

        var budgetPeriod = PeriodGranularity switch
        {
            "Monthly" => BudgetPeriod.Monthly,
            "Quarterly" => BudgetPeriod.Quarterly,
            "Semester" => BudgetPeriod.Semester,
            "Annual" => BudgetPeriod.Annual,
            _ => BudgetPeriod.Monthly // fallback for unknown values
        };

        // Use reflection to set CreatedAt/UpdatedAt on the entity
        // (Budget constructor doesn't accept timestamps; they come from the base Entity)
        var budget = new Budget(
            new BudgetId(Guid.Parse(Id)),
            new UserId(UserId),
            new CategoryId(Guid.Parse(CategoryId)),
            effectiveFrom,
            effectiveUntil,
            budgetPeriod,
            new Money(LimitAmount, Currency));

        // Set base entity timestamps via reflection
        // (CreatedAt/UpdatedAt are protected setters on Entity<TId>)
        var entityType = typeof(Domain.Common.Entity<BudgetId>);
        var createdAtProp = entityType.GetProperty("CreatedAt");
        var updatedAtProp = entityType.GetProperty("UpdatedAt");

        if (createdAtProp != null)
            createdAtProp.SetValue(budget, CreatedAt);
        if (updatedAtProp != null)
            updatedAtProp.SetValue(budget, UpdatedAt);

        return budget;
    }

    /// <summary>
    /// Maps a domain Budget entity to this infrastructure row.
    /// EffectiveFrom/EffectiveUntil are stored as DATE — use .Date to strip time.
    /// </summary>
    public static BudgetRow FromDomain(Budget b)
    {
        return new BudgetRow
        {
            Id = b.Id.Value.ToString(),
            UserId = b.UserId.Value,
            CategoryId = b.CategoryId.Value.ToString(),
            EffectiveFrom = b.EffectiveFrom.ToDateTime(TimeOnly.MinValue),
            EffectiveUntil = b.EffectiveUntil.HasValue
                ? b.EffectiveUntil.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            PeriodGranularity = b.PeriodGranularity.ToString(),
            LimitAmount = b.Limit.Amount,
            Currency = b.Limit.Currency,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }

    /// <summary>
    /// Converts budget to insert-safe DTO (excludes server-managed timestamps).
    /// Timestamps are assigned by database triggers, not by client.
    /// </summary>
    public static BudgetRow FromDomainForInsert(Budget b)
    {
        return new BudgetRow
        {
            Id = b.Id.Value.ToString(),
            UserId = b.UserId.Value,
            CategoryId = b.CategoryId.Value.ToString(),
            EffectiveFrom = b.EffectiveFrom.ToDateTime(TimeOnly.MinValue),
            EffectiveUntil = b.EffectiveUntil.HasValue
                ? b.EffectiveUntil.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            PeriodGranularity = b.PeriodGranularity.ToString(),
            LimitAmount = b.Limit.Amount,
            Currency = b.Limit.Currency
            // NOTE: Do NOT set CreatedAt or UpdatedAt - let database triggers handle timestamps
        };
    }
}

/// <summary>
/// Supabase implementation of IBudgetRepository.
/// Redesigned for permanent policy budgets with configurable period granularity.
/// </summary>
public class SupabaseBudgetRepository : IBudgetRepository
{
    private readonly Supabase.Client _client;

    public SupabaseBudgetRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<Budget?> GetByIdAsync(BudgetId id)
    {
        var idString = id.Value.ToString();
        var response = await _client.From<BudgetRow>()
            .Where(r => r.Id == idString)
            .Get();
        var row = response.Models.FirstOrDefault();
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId)
    {
        var response = await _client.From<BudgetRow>()
            .Where(r => r.UserId == userId.Value)
            .Get();
        return response.Models.Select(r => r.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Budget>> GetByUserAndCategoryAsync(
        UserId userId, CategoryId categoryId)
    {
        var categoryIdString = categoryId.Value.ToString();
        var response = await _client.From<BudgetRow>()
            .Where(r => r.UserId == userId.Value)
            .Where(r => r.CategoryId == categoryIdString)
            .Get();
        return response.Models.Select(r => r.ToDomain()).ToList();
    }

    /// <summary>
    /// Returns the budget active for a given user, category, and date.
    /// A budget is active when asOf falls within [effective_from, effective_until]
    /// (or effective_from onward when effective_until is null).
    ///
    /// Query strategy: filter by user_id + category_id, then filter in memory
    /// for the date range because Postgrest C# client does not support OR conditions
    /// in .Where() lambdas, making a single-query range filter impractical.
    /// The Domain exclusion constraint ensures at most one row matches.
    /// </summary>
    public async Task<Budget?> GetActiveByUserAndCategoryAsync(
        UserId userId, CategoryId categoryId, DateOnly asOf)
    {
        var categoryIdString = categoryId.Value.ToString();

        // Fetch all budgets for this user+category (exclusion constraint
        // guarantees at most ~1 active at any point; typically just a handful
        // of historical rows).
        var response = await _client.From<BudgetRow>()
            .Where(r => r.UserId == userId.Value)
            .Where(r => r.CategoryId == categoryIdString)
            .Get();

        var matchingRow = response.Models.FirstOrDefault(row =>
        {
            var effectiveFrom = DateOnly.FromDateTime(row.EffectiveFrom);
            if (asOf < effectiveFrom)
                return false;

            if (row.EffectiveUntil.HasValue)
            {
                var effectiveUntil = DateOnly.FromDateTime(row.EffectiveUntil.Value);
                return asOf <= effectiveUntil;
            }

            return true; // permanent budget (effective_until IS NULL)
        });

        return matchingRow?.ToDomain();
    }

    /// <summary>
    /// Returns all budgets for a user whose effective date range overlaps
    /// with the query range [from, to]. If 'to' is null, treats the query
    /// range as open-ended (from onward).
    ///
    /// Filtering is performed in memory after fetching user budgets because
    /// the Postgrest C# client does not support OR conditions or method calls
    /// inside .Where() lambdas.
    /// </summary>
    public async Task<IReadOnlyList<Budget>> GetByUserAndDateRangeAsync(
        UserId userId, DateOnly from, DateOnly? to)
    {
        // Fetch all budgets for this user — budgets are per-user and typically
        // few in number (dozens, not thousands). Filter overlap in memory.
        var response = await _client.From<BudgetRow>()
            .Where(r => r.UserId == userId.Value)
            .Get();

        var queryEnd = to ?? DateOnly.MaxValue;

        var matchingRows = response.Models.Where(row =>
        {
            var rowStart = DateOnly.FromDateTime(row.EffectiveFrom);
            var rowEnd = row.EffectiveUntil.HasValue
                ? DateOnly.FromDateTime(row.EffectiveUntil.Value)
                : DateOnly.MaxValue;

            // Two ranges [rowStart, rowEnd] and [from, queryEnd] overlap
            // when rowStart <= queryEnd AND rowEnd >= from
            return rowStart <= queryEnd && rowEnd >= from;
        });

        return matchingRows.Select(r => r.ToDomain()).ToList();
    }

    public async Task AddAsync(Budget budget)
    {
        var row = BudgetRow.FromDomainForInsert(budget);
        await _client.From<BudgetRow>().Insert(row);
    }

    public async Task UpdateAsync(Budget budget)
    {
        var idString = budget.Id.Value.ToString();
        var row = BudgetRow.FromDomain(budget);
        await _client.From<BudgetRow>()
            .Where(r => r.Id == idString)
            .Update(row);
    }

    public async Task DeleteAsync(BudgetId id)
    {
        var idString = id.Value.ToString();
        await _client.From<BudgetRow>()
            .Where(r => r.Id == idString)
            .Delete();
    }
}

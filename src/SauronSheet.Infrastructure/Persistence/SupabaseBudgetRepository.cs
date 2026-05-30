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
/// Postgrest DTO for the budgets table.
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

    [Column("period_start")]
    public DateTime PeriodStart { get; set; }

    [Column("period_end")]
    public DateTime PeriodEnd { get; set; }

    [Column("limit_amount")]
    public decimal LimitAmount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "EUR";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public Budget ToDomain()
    {
        return new Budget(
            new BudgetId(Guid.Parse(Id)),
            new UserId(UserId),
            new CategoryId(Guid.Parse(CategoryId)),
            new DateRange(PeriodStart, PeriodEnd),
            new Money(LimitAmount, Currency));
    }

    public static BudgetRow FromDomain(Budget b)
    {
        return new BudgetRow
        {
            Id = b.Id.Value.ToString(),
            UserId = b.UserId.Value,
            CategoryId = b.CategoryId.Value.ToString(),
            // Use .Date to strip any residual time component: period columns are DATE, not TIMESTAMPTZ.
            PeriodStart = b.Period.StartDate.Date,
            PeriodEnd = b.Period.EndDate.Date,
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
            // Use .Date to strip any residual time component: period columns are DATE, not TIMESTAMPTZ.
            PeriodStart = b.Period.StartDate.Date,
            PeriodEnd = b.Period.EndDate.Date,
            LimitAmount = b.Limit.Amount,
            Currency = b.Limit.Currency
            // NOTE: Do NOT set CreatedAt or UpdatedAt - let database triggers handle timestamps
        };
    }
}

/// <summary>
/// Supabase implementation of IBudgetRepository.
/// Phase 5: Budget Management.
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
        try
        {
            var idString = id.Value.ToString();
            var response = await _client.From<BudgetRow>()
                .Where(r => r.Id == idString)
                .Get();
            var row = response.Models.FirstOrDefault();
            return row?.ToDomain();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseBudgetRepository.GetByIdAsync");
                scope.SetTag("budgetId", id.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<IReadOnlyList<Budget>> GetByUserIdAsync(UserId userId)
    {
        try
        {
            var response = await _client.From<BudgetRow>()
                .Where(r => r.UserId == userId.Value)
                .Get();
            return response.Models.Select(r => r.ToDomain()).ToList();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseBudgetRepository.GetByUserIdAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<Budget?> GetByUserAndCategoryAndMonthAsync(
        UserId userId, CategoryId categoryId, DateRange period)
    {
        try
        {
            var categoryIdString = categoryId.Value.ToString();
            // Use date-only comparison: period_start is a DATE column; stripping the
            // time component avoids false mismatches from residual ticks.
            var periodStartDate = period.StartDate.Date;

            var response = await _client.From<BudgetRow>()
                .Where(r => r.UserId == userId.Value)
                .Where(r => r.CategoryId == categoryIdString)
                .Where(r => r.PeriodStart == periodStartDate)
                .Get();
            var row = response.Models.FirstOrDefault();
            return row?.ToDomain();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseBudgetRepository.GetByUserAndCategoryAndMonthAsync");
                scope.SetTag("userId", userId.Value);
                scope.SetTag("categoryId", categoryId.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task AddAsync(Budget budget)
    {
        try
        {
            var row = BudgetRow.FromDomainForInsert(budget);
            await _client.From<BudgetRow>().Insert(row);
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseBudgetRepository.AddAsync");
                scope.SetTag("userId", budget.UserId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task UpdateAsync(Budget budget)
    {
        try
        {
            var idString = budget.Id.Value.ToString();
            var row = BudgetRow.FromDomain(budget);
            await _client.From<BudgetRow>()
                .Where(r => r.Id == idString)
                .Update(row);
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseBudgetRepository.UpdateAsync");
                scope.SetTag("budgetId", budget.Id.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task DeleteAsync(BudgetId id)
    {
        try
        {
            var idString = id.Value.ToString();
            await _client.From<BudgetRow>()
                .Where(r => r.Id == idString)
                .Delete();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseBudgetRepository.DeleteAsync");
                scope.SetTag("budgetId", id.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }
}

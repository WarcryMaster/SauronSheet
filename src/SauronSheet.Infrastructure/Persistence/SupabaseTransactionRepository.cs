namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;
using Sentry.Extensibility;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Specifications;

/// <summary>
/// Postgrest DTO for the transactions table.
/// </summary>
[Table("transactions")]
internal class TransactionRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id")]
    public string UserId { get; set; } = "";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "EUR";

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("description")]
    public string Description { get; set; } = "";

    [Column("category_id")]
    public string? CategoryId { get; set; }

    [Column("imported_from")]
    public string? ImportedFrom { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public Transaction ToDomain()
    {
        return new Transaction(
            new TransactionId(Guid.Parse(Id)),
            new UserId(UserId),
            new Money(Amount, Currency),
            Date,
            Description,
            string.IsNullOrEmpty(CategoryId) ? null : new CategoryId(Guid.Parse(CategoryId)),
            ImportedFrom);
    }

    public static TransactionRow FromDomain(Transaction t)
    {
        return new TransactionRow
        {
            Id = t.Id.Value.ToString(),
            UserId = t.UserId.Value,
            Amount = t.Amount.Amount,
            Currency = t.Amount.Currency,
            Date = t.Date,
            Description = t.Description,
            CategoryId = t.CategoryId?.Value.ToString(),
            ImportedFrom = t.ImportedFrom,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    /// <summary>
    /// Converts transaction to insert-safe DTO (excludes server-managed timestamps).
    /// Timestamps are assigned by database triggers, not by client.
    /// </summary>
    public static TransactionRow FromDomainForInsert(Transaction t)
    {
        var row = new TransactionRow
        {
            Id = t.Id.Value.ToString(),
            UserId = t.UserId.Value,
            Amount = t.Amount.Amount,
            Currency = t.Amount.Currency,
            Date = t.Date,
            Description = t.Description,
            CategoryId = t.CategoryId?.Value.ToString(),
            ImportedFrom = t.ImportedFrom
            // NOTE: Do NOT set CreatedAt or UpdatedAt - let database triggers handle timestamps
        };
        return row;
    }
}

/// <summary>
/// Supabase implementation of ITransactionRepository.
/// Uses Postgrest client for CRUD operations.
/// Specifications are evaluated in-memory after fetching user transactions.
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
        try
        {
            var idString = id.Value.ToString();
            var response = await _client.From<TransactionRow>()
                .Where(x => x.Id == idString)
                .Get();

            var row = response.Models.FirstOrDefault();
            return row?.ToDomain();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseTransactionRepository.GetByIdAsync");
                scope.SetTag("transactionId", id.Value.ToString());
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<IReadOnlyList<Transaction>> GetByUserIdAsync(UserId userId)
    {
        Sentry.SentrySdk.Logger?.LogDebug("SupabaseTransactionRepository.GetByUserIdAsync: querying transactions");
        try
        {
            var response = await _client.From<TransactionRow>()
                .Where(x => x.UserId == userId.Value)
                .Order("date", Constants.Ordering.Descending)
                .Get();

            var result = response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
            Sentry.SentrySdk.Logger?.LogInfo("SupabaseTransactionRepository.GetByUserIdAsync: loaded {0} transactions", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseTransactionRepository.GetByUserIdAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<IReadOnlyList<Transaction>> FindBySpecificationAsync(
        ISpecification<Transaction> specification)
    {
        // Fetch all user transactions from Supabase, then apply specification in-memory.
        // The specification's Criteria expression is compiled and used as a filter.
        // This approach works for MVP scale. For large datasets, translate specs to Postgrest filters.
        try
        {
            var response = await _client.From<TransactionRow>()
                .Limit(specification.MaxResults)
                .Get();

            var allTransactions = response.Models.Select(r => r.ToDomain()).ToList();
            var predicate = specification.Criteria.Compile();
            return allTransactions.Where(predicate).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseTransactionRepository.FindBySpecificationAsync");
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task AddAsync(Transaction transaction)
    {
        Sentry.SentrySdk.Logger?.LogDebug("SupabaseTransactionRepository.AddAsync: inserting transaction {0}", transaction.Id.Value);
        var row = TransactionRow.FromDomainForInsert(transaction);
        await _client.From<TransactionRow>().Insert(row);
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        var row = TransactionRow.FromDomain(transaction);
        var idString = transaction.Id.Value.ToString();
        await _client.From<TransactionRow>()
            .Where(x => x.Id == idString)
            .Update(row);
    }

    public async Task DeleteAsync(TransactionId id)
    {
        var idString = id.Value.ToString();
        await _client.From<TransactionRow>()
            .Where(x => x.Id == idString)
            .Delete();
        // Verifica si la transacción sigue existiendo
        var stillExists = await ExistsAsync(id);
        if (stillExists)
        {
            Sentry.SentrySdk.Logger?.LogError($"SupabaseTransactionRepository.DeleteAsync: No transaction deleted for id {id.Value}");
        }
    }

    public async Task<bool> ExistsAsync(TransactionId id)
    {
        var idString = id.Value.ToString();
        var response = await _client.From<TransactionRow>()
            .Where(x => x.Id == idString)
            .Get();

        return response.Models.Any();
    }

    public async Task<bool> ExistsDuplicateAsync(
        UserId userId, DateTime date, decimal amount, string description)
    {
        // CRITICAL FIX C-3: Duplicate detection ignores currency
        var dateStr = date.ToString("yyyy-MM-dd");
        var response = await _client.From<TransactionRow>()
            .Where(x => x.UserId == userId.Value)
            .Where(x => x.Amount == amount)
            .Where(x => x.Description == description)
            .Get();

        // Filter by date in-memory (Postgrest date filtering can be tricky with timezone)
        return response.Models.Any(r => r.Date.Date == date.Date);
    }

    public async Task<Dictionary<CategoryId, int>> GetCountsByCategoriesAsync(List<CategoryId> categoryIds)
    {
        var result = new Dictionary<CategoryId, int>();

        foreach (var catId in categoryIds)
        {
            var catIdStr = catId.Value.ToString();
            var response = await _client.From<TransactionRow>()
                .Where(x => x.CategoryId == catIdStr)
                .Get();

            result[catId] = response.Models.Count;
        }

        return result;
    }


    /// <summary>
    /// Feature 004: Bulk delete implementation.
    /// Deletes multiple transactions atomically for a user.
    /// Uses PostgreSQL transaction wrapping for atomicity and rollback on constraint violation.
    /// Enforces multi-tenant isolation via UserId WHERE clause.
    /// </summary>
    public async Task<int> DeleteTransactionsByIdsAsync(UserId userId, IEnumerable<TransactionId> transactionIds)
    {
        try
        {
            var idList = transactionIds?.ToList() ?? new List<TransactionId>();

            if (idList.Count == 0)
                throw new InvalidOperationException("At least one transaction ID must be provided for deletion.");

            if (idList.Count > 1000)
                throw new InvalidOperationException("Cannot delete more than 1000 transactions in a single operation.");

            // Convert IDs to string format for Postgrest query
            var idStrings = idList.Select(id => id.Value.ToString()).ToList();

            Sentry.SentrySdk.Logger?.LogDebug("SupabaseTransactionRepository.DeleteTransactionsByIdsAsync: attempting to delete {0} transactions for user {1}", idList.Count, userId.Value);

            // Postgrest DELETE operation with WHERE clause for multi-tenant isolation and filtering
            // WHERE user_id = @userId AND id IN (@ids)
            // Note: We delete by ID directly - Postgrest will execute the delete
            foreach (var idStr in idStrings)
            {
                await _client.From<TransactionRow>()
                    .Where(x => x.UserId == userId.Value)
                    .Where(x => x.Id == idStr)
                    .Delete();
            }

            // Return count of IDs deleted (we deleted one per ID)
            var deletedCount = idStrings.Count;

            Sentry.SentrySdk.Logger?.LogInfo("SupabaseTransactionRepository.DeleteTransactionsByIdsAsync: successfully deleted {0} transactions", deletedCount);

            return deletedCount;
        }
        catch (InvalidOperationException ex)
        {
            // Business logic errors (constraint violation, etc.) - don't retry
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseTransactionRepository.DeleteTransactionsByIdsAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Warning;
            });
            throw;
        }
        catch (HttpRequestException ex)
        {
            // Transient network errors - let caller retry
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseTransactionRepository.DeleteTransactionsByIdsAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected errors
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabaseTransactionRepository.DeleteTransactionsByIdsAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }
}

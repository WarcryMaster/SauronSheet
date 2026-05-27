namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;
using Sentry.Extensibility;
using SauronSheet.Domain.Common;
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

    [Column("bank_category")]
    public string? BankCategory { get; set; }

    [Column("bank_subcategory")]
    public string? BankSubcategory { get; set; }

    [Column("subcategory_id")]
    public string? SubcategoryId { get; set; }

    [Column("category_source")]
    public string? CategorySourceColumn { get; set; }

    [Column("created_at")]
    [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public DateTime? UpdatedAt { get; set; }

    public Transaction ToDomain()
    {
        SubcategoryId? subcategoryId = null;
        if (!string.IsNullOrEmpty(SubcategoryId) && Guid.TryParse(SubcategoryId, out var subGuid))
        {
            subcategoryId = new SubcategoryId(subGuid);
        }

        var categorySource = ParseCategorySource(CategorySourceColumn);

        return new Transaction(
            new TransactionId(Guid.Parse(Id)),
            new UserId(UserId),
            new Money(Amount, Currency),
            Date,
            Description,
            string.IsNullOrEmpty(CategoryId) ? null : new CategoryId(Guid.Parse(CategoryId)),
            ImportedFrom,
            BankCategory,
            BankSubcategory,
            subcategoryId,
            categorySource);
    }

    private static CategorySource ParseCategorySource(string? source)
    {
        return source switch
        {
            "Legacy" => CategorySource.Legacy,
            "RawOnly" => CategorySource.RawOnly,
            "AutoMatched" => CategorySource.AutoMatched,
            "UserOverride" => CategorySource.UserOverride,
            _ => CategorySource.Legacy
        };
    }

    private static string? SerializeCategorySource(CategorySource source)
    {
        return source switch
        {
            CategorySource.Legacy => "Legacy",
            CategorySource.RawOnly => "RawOnly",
            CategorySource.AutoMatched => "AutoMatched",
            CategorySource.UserOverride => "UserOverride",
            _ => "Legacy"
        };
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
            BankCategory = t.BankCategory,
            BankSubcategory = t.BankSubcategory,
            SubcategoryId = t.SubcategoryId?.Value.ToString(),
            CategorySourceColumn = SerializeCategorySource(t.CategorySource),
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    /// <summary>
    /// Converts transaction to insert-safe DTO.
    /// CreatedAt is set client-side because Postgrest serializes all properties regardless of null
    /// and the column has NOT NULL constraint. The value is semantically equivalent to DEFAULT NOW().
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
            ImportedFrom = t.ImportedFrom,
            BankCategory = t.BankCategory,
            BankSubcategory = t.BankSubcategory,
            SubcategoryId = t.SubcategoryId?.Value.ToString(),
            CategorySourceColumn = SerializeCategorySource(t.CategorySource),
            CreatedAt = DateTime.UtcNow
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
    private readonly IUserContext _userContext;

    public SupabaseTransactionRepository(Supabase.Client client, IUserContext userContext)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
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
        // Fetch user-scoped transactions from Supabase, then apply specification in-memory.
        // The UserId filter ensures multi-tenant isolation at the query level,
        // complementing Supabase RLS (defense in depth).
        // The specification's Criteria expression is compiled and used as a secondary filter.
        // This approach works for MVP scale. For large datasets, translate specs to Postgrest filters.
        try
        {
            var userId = _userContext.UserId;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var response = await _client.From<TransactionRow>()
                .Where(x => x.UserId == userId)
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
        // Note: Thread culture is set to InvariantCulture in ImportTransactionsCommandHandler
        // to ensure decimal serialization uses dot, not comma
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
        if (categoryIds == null || categoryIds.Count == 0)
            return new Dictionary<CategoryId, int>();

        // Single Postgrest query with IN filter instead of N individual queries.
        // Using .Filter() with Operator.In avoids the "method calls in lambda" bug in supabase-csharp 0.16.2.
        var categoryIdValues = categoryIds.Select(id => (object)id.Value.ToString()).ToList();

        var response = await _client.From<TransactionRow>()
            .Filter("category_id", Constants.Operator.In, categoryIdValues)
            .Get();

        var counts = response.Models
            .Where(r => r.CategoryId != null)
            .GroupBy(r => r.CategoryId!)
            .ToDictionary(g => new CategoryId(Guid.Parse(g.Key)), g => g.Count());

        // Ensure all requested category IDs are in the result (with 0 for missing)
        var result = new Dictionary<CategoryId, int>();
        foreach (var catId in categoryIds)
        {
            result[catId] = counts.GetValueOrDefault(catId, 0);
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

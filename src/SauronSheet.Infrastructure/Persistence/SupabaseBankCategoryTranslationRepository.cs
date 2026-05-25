namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Repositories;
using Mapping;
using Postgrest;

/// <summary>
/// Supabase implementation of IBankCategoryTranslationRepository.
/// Read-only repository for bank category translation overrides.
/// Uses Postgrest client to query the bank_category_translations table.
/// </summary>
public class SupabaseBankCategoryTranslationRepository : IBankCategoryTranslationRepository
{
    private readonly Supabase.Client _client;

    public SupabaseBankCategoryTranslationRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Protected no-arg constructor for test subclasses that override all database methods.
    /// The client field is null — safe only when ExecuteExactMatchQueryAsync and
    /// ExecuteGenericMatchQueryAsync are overridden so they never reach the Supabase client.
    /// </summary>
    protected SupabaseBankCategoryTranslationRepository()
    {
        _client = null!;
    }

    public async Task<BankCategoryTranslation?> FindByBankCategoryAsync(string bankCategory, string? bankSubcategory)
    {
        try
        {
            var bankCat = bankCategory ?? string.Empty;

            // Postgrest C# client does not support OR conditions.
            // We need to handle two cases:
            // 1. Exact match on both bank_category AND bank_subcategory
            // 2. Match on bank_category with bank_subcategory IS NULL
            // Since OR is unsupported, use separate queries and combine.
            //
            // CR-2e: exact match (bank_category + bank_subcategory) MUST be evaluated FIRST.
            // Generic (bank_subcategory IS NULL) is used ONLY as a fallback when no exact match exists.

            // Query 1: Exact match — if bankSubcategory provided, try bank_category + bank_subcategory first
            if (!string.IsNullOrEmpty(bankSubcategory))
            {
                var exactRow = await ExecuteExactMatchQueryAsync(bankCat, bankSubcategory);
                if (exactRow != null)
                    return exactRow.ToDomain();
            }

            // Query 2: Generic fallback — bank_category only, bank_subcategory IS NULL
            var genericRow = await ExecuteGenericMatchQueryAsync(bankCat);
            if (genericRow != null)
                return genericRow.ToDomain();

            return null;
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("repo", "SupabaseBankCategoryTranslationRepository.FindByBankCategoryAsync");
                scope.SetTag("bankCategory", bankCategory ?? "(null)");
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    /// <summary>
    /// Executes the exact-match query (bank_category + bank_subcategory).
    /// Internal virtual to allow test subclasses (via InternalsVisibleTo) to substitute
    /// in-memory data without requiring a live Supabase client.
    /// </summary>
    internal virtual async Task<BankCategoryTranslationRow?> ExecuteExactMatchQueryAsync(
        string bankCategory, string bankSubcategory)
    {
        var bankSub = bankSubcategory;
        var response = await _client.From<BankCategoryTranslationRow>()
            .Where(x => x.BankCategory == bankCategory)
            .Where(x => x.BankSubcategory == bankSub)
            .Get();
        return response.Models.Count > 0 ? response.Models.First() : null;
    }

    /// <summary>
    /// Executes the generic-fallback query (bank_category only, bank_subcategory IS NULL).
    /// Internal virtual to allow test subclasses (via InternalsVisibleTo) to substitute in-memory data.
    /// </summary>
    internal virtual async Task<BankCategoryTranslationRow?> ExecuteGenericMatchQueryAsync(
        string bankCategory)
    {
        var response = await _client.From<BankCategoryTranslationRow>()
            .Where(x => x.BankCategory == bankCategory)
            .Where(x => x.BankSubcategory == null)
            .Get();
        return response.Models.Count > 0 ? response.Models.First() : null;
    }
}

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

            // Query 1: Match on bank_category only, with bank_subcategory IS NULL
            var nullSubResponse = await _client.From<BankCategoryTranslationRow>()
                .Where(x => x.BankCategory == bankCat)
                .Where(x => x.BankSubcategory == null)
                .Get();

            if (nullSubResponse.Models.Count > 0)
                return nullSubResponse.Models.First().ToDomain();

            // Query 2: If bankSubcategory is specified, try exact match
            if (!string.IsNullOrEmpty(bankSubcategory))
            {
                var bankSub = bankSubcategory;
                var exactResponse = await _client.From<BankCategoryTranslationRow>()
                    .Where(x => x.BankCategory == bankCat)
                    .Where(x => x.BankSubcategory == bankSub)
                    .Get();

                if (exactResponse.Models.Count > 0)
                    return exactResponse.Models.First().ToDomain();
            }

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
}

namespace SauronSheet.Domain.Repositories;

using System.Threading.Tasks;

/// <summary>
/// A resolved bank category translation result.
/// Maps raw bank category/subcategory values to resolved display names.
/// </summary>
public record BankCategoryTranslation(
    string BankCategory,
    string? BankSubcategory,
    string ResolvedCategoryName,
    string? ResolvedSubcategoryName);

/// <summary>
/// Read-only repository for bank category translation overrides.
/// Allows users to map raw bank values to specific category names.
/// Implemented in Infrastructure using Supabase/Postgrest.
/// </summary>
public interface IBankCategoryTranslationRepository
{
    Task<BankCategoryTranslation?> FindByBankCategoryAsync(string bankCategory, string? bankSubcategory);
}

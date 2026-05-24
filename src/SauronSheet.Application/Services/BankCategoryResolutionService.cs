namespace SauronSheet.Application.Services;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using Domain.ValueObjects;

/// <summary>
/// Resolution service that matches raw bank category/subcategory values
/// against user categories and optional translation overrides.
/// 
/// Algorithm:
/// 1. Normalize input (trim, empty guard).
/// 2. Query bank_category_translations for exact match.
/// 3. If translation found, use resolved_category_name; otherwise use raw bank category.
/// 4. Fetch user's categories and match by name (case-insensitive, in-memory).
/// 5. If category found and subcategory provided, match subcategory within category.
/// 6. Return ResolutionResult with appropriate source.
/// </summary>
public class BankCategoryResolutionService : IBankCategoryResolutionService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubcategoryRepository _subcategoryRepository;
    private readonly IBankCategoryTranslationRepository _translationRepository;

    public BankCategoryResolutionService(
        ICategoryRepository categoryRepository,
        ISubcategoryRepository subcategoryRepository,
        IBankCategoryTranslationRepository translationRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _subcategoryRepository = subcategoryRepository ?? throw new ArgumentNullException(nameof(subcategoryRepository));
        _translationRepository = translationRepository ?? throw new ArgumentNullException(nameof(translationRepository));
    }

    public async Task<ResolutionResult> ResolveAsync(
        UserId userId, string? bankCategory, string? bankSubcategory, CancellationToken ct)
    {
        // Step 1: Normalize input
        if (string.IsNullOrWhiteSpace(bankCategory))
            return new ResolutionResult(null, null, CategorySource.RawOnly);

        var normalizedCategory = bankCategory.Trim();
        var normalizedSubcategory = bankSubcategory?.Trim();

        // Step 2: Check bank_category_translations for override
        var translation = await _translationRepository.FindByBankCategoryAsync(
            normalizedCategory, normalizedSubcategory);

        var resolvedName = translation?.ResolvedCategoryName ?? normalizedCategory;

        // Step 3: Fetch user categories and match by name (case-insensitive)
        var userCategories = await _categoryRepository.GetByUserIdAsync(userId);
        var match = userCategories.FirstOrDefault(
            c => c.Name.Value.Equals(resolvedName, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return new ResolutionResult(null, null, CategorySource.RawOnly);

        // Step 4: Match subcategory within category
        SubcategoryId? subcategoryId = null;
        if (!string.IsNullOrWhiteSpace(normalizedSubcategory))
        {
            var subcats = await _subcategoryRepository.GetByCategoryIdAsync(match.Id);
            var subMatch = subcats.FirstOrDefault(
                s => s.Name.Value.Equals(normalizedSubcategory, StringComparison.OrdinalIgnoreCase));
            if (subMatch != null)
                subcategoryId = subMatch.Id;
        }

        return new ResolutionResult(match.Id, subcategoryId, CategorySource.AutoMatched);
    }
}

namespace SauronSheet.Application.Services;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Exceptions;
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

    // ════════════════════════════════════════════════════════════════════════
    // ResolveOrCreateAsync — get-or-add for bank statement import (PCE-3 / PCE-4)
    // ════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ResolutionResult> ResolveOrCreateAsync(
        UserId userId, string? rawCategory, string? rawSubcategory, CancellationToken ct)
    {
        // PCE-3d: null / whitespace rawCategory → RawOnly, nothing created
        if (string.IsNullOrWhiteSpace(rawCategory))
            return new ResolutionResult(null, null, CategorySource.RawOnly);

        var rawCat       = rawCategory.Trim();
        var normalizedCat = CategoryNormalizer.Normalize(rawCat)!; // safe: rawCat is not whitespace

        // PCE-3a/3b/3c: find or create user category
        var category = await FindOrCreateCategoryAsync(userId, rawCat, normalizedCat);

        // PCE-4a/4b/4c/4d: find or create subcategory (scoped by categoryId)
        SubcategoryId? subcategoryId = null;
        if (!string.IsNullOrWhiteSpace(rawSubcategory))
        {
            var rawSub        = rawSubcategory.Trim();
            var normalizedSub = CategoryNormalizer.Normalize(rawSub)!;
            subcategoryId     = await FindOrCreateSubcategoryAsync(userId, category.Id, rawSub, normalizedSub);
        }

        return new ResolutionResult(category.Id, subcategoryId, CategorySource.AutoMatched);
    }

    /// <summary>
    /// Find an existing user-owned category by normalized key, or create a new one.
    /// PCE-3c: system defaults are bypassed — a user-owned category is always returned.
    /// PCE-3e: concurrent 23505 → <see cref="DuplicateEntityException"/> → retry-get.
    /// </summary>
    private async Task<Category> FindOrCreateCategoryAsync(
        UserId userId, string rawName, string normalizedName)
    {
        var existing = await _categoryRepository.FindByNormalizedNameAndUserAsync(userId, normalizedName);

        // PCE-3a: found and user-owned → reuse
        // PCE-3c: found but system default → bypass (system defaults must never enter import path)
        if (existing != null && !existing.IsSystemDefault)
            return existing;

        // PCE-3b / PCE-3c: not found (or system default bypassed) → create user category
        var safeName    = rawName.Length > CategoryName.MaxLength ? rawName[..CategoryName.MaxLength] : rawName;
        var newCategory = new Category(
            new CategoryId(Guid.NewGuid()),
            userId,
            CategoryName.Create(safeName),
            CategoryType.Expense,
            ColorHex.Create("#607D8B"),
            "tag");

        try
        {
            await _categoryRepository.AddAsync(newCategory, normalizedName);
            return newCategory;
        }
        catch (DuplicateEntityException)
        {
            // PCE-3e: concurrent insert won the race — retry-get to return the winner
            var retried = await _categoryRepository.FindByNormalizedNameAndUserAsync(userId, normalizedName);
            return retried ?? throw new InvalidOperationException(
                $"Category '{normalizedName}' not found after 23505 conflict. Data integrity error.");
        }
    }

    /// <summary>
    /// Find an existing subcategory by normalized key scoped to (userId, categoryId),
    /// or create a new one with <see cref="Subcategory.IsAutoCreated"/> = true.
    /// PCE-4d: scope is (userId, categoryId), not global.
    /// </summary>
    private async Task<SubcategoryId?> FindOrCreateSubcategoryAsync(
        UserId userId, CategoryId categoryId, string rawName, string normalizedName)
    {
        var existing = await _subcategoryRepository.FindByNormalizedNameAsync(userId, categoryId, normalizedName);
        if (existing != null)
            return existing.Id;

        var safeName       = rawName.Length > SubcategoryName.MaxLength ? rawName[..SubcategoryName.MaxLength] : rawName;
        var newSubcategory = new Subcategory(
            new SubcategoryId(Guid.NewGuid()),
            userId,
            categoryId,
            SubcategoryName.Create(safeName),
            isAutoCreated: true);

        try
        {
            await _subcategoryRepository.AddAsync(newSubcategory, normalizedName);
            return newSubcategory.Id;
        }
        catch (DuplicateEntityException)
        {
            // Concurrent insert — retry-get
            var retried = await _subcategoryRepository.FindByNormalizedNameAsync(userId, categoryId, normalizedName);
            return retried?.Id;
        }
    }
}

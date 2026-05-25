namespace SauronSheet.Application.Services;

using System.Threading;
using System.Threading.Tasks;
using Domain.ValueObjects;

/// <summary>
/// Service that resolves raw bank category/subcategory values from a PDF parser
/// to actual Category/Subcategory entities owned by the user.
/// 
/// Resolution algorithm:
/// 1. Check bank_category_translations table for an override.
/// 2. If found, use the resolved name; otherwise use raw bank category.
/// 3. Match by name against user's categories (case-insensitive).
/// 4. If subcategory provided, match within the resolved category.
/// 5. Returns ResolutionResult with CategoryId, SubcategoryId (or null) and CategorySource.
/// </summary>
public interface IBankCategoryResolutionService
{
    /// <summary>
    /// Resolves a raw bank category/subcategory pair to a user's category/subcategory.
    /// </summary>
    /// <param name="userId">The current user's ID.</param>
    /// <param name="bankCategory">Raw bank category from PDF (e.g. "Compras", "Aliment.").</param>
    /// <param name="bankSubcategory">Optional raw bank subcategory from PDF.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>ResolutionResult with resolved IDs and source type.</returns>
    Task<ResolutionResult> ResolveAsync(UserId userId, string? bankCategory, string? bankSubcategory, CancellationToken ct);

    /// <summary>
    /// Get-or-add: resolves a raw bank category/subcategory to an existing user category,
    /// creating one if it does not exist. System defaults are never reused — a user-owned
    /// category is always created for PDF-imported data.
    ///
    /// Spec: PCE-3 / PCE-4. Deduplication via CategoryNormalizer.Normalize() key.
    /// Concurrent inserts are handled via catch-23505 + retry-get.
    /// </summary>
    /// <param name="userId">The current user's ID.</param>
    /// <param name="rawCategory">Raw category literal from PDF (e.g. "Viajes y turismo").</param>
    /// <param name="rawSubcategory">Optional raw subcategory literal from PDF.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// ResolutionResult with CategoryId + SubcategoryId resolved/created (AutoMatched),
    /// or RawOnly when rawCategory is null/whitespace.
    /// </returns>
    Task<ResolutionResult> ResolveOrCreateAsync(UserId userId, string? rawCategory, string? rawSubcategory, CancellationToken ct);
}

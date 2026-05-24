namespace SauronSheet.Infrastructure.Mapping;

using System;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Persistence;

/// <summary>
/// Extension methods for mapping between Postgrest DTOs and Domain objects.
/// Keeps mapping logic testable and separate from repository implementations.
/// </summary>
internal static class MappingExtensions
{
    // ── SubcategoryRow ↔ Subcategory ─────────────────────────────────────────

    /// <summary>
    /// Maps a SubcategoryRow (Postgrest DTO) to a Subcategory domain entity.
    /// </summary>
    public static Subcategory ToDomain(this SubcategoryRow row)
    {
        UserId? userId = string.IsNullOrEmpty(row.UserId)
            ? null
            : new UserId(row.UserId);

        return new Subcategory(
            new SubcategoryId(Guid.Parse(row.Id)),
            userId,
            new CategoryId(Guid.Parse(row.CategoryId)),
            SubcategoryName.Create(row.Name),
            row.IsAutoCreated);
    }

    /// <summary>
    /// Maps a Subcategory domain entity to a SubcategoryRow (Postgrest DTO).
    /// </summary>
    public static SubcategoryRow FromDomain(this Subcategory subcategory)
    {
        return new SubcategoryRow
        {
            Id = subcategory.Id.Value.ToString(),
            UserId = subcategory.UserId?.Value,
            CategoryId = subcategory.CategoryId.Value.ToString(),
            Name = subcategory.Name.Value,
            IsAutoCreated = subcategory.IsAutoCreated
        };
    }

    // ── BankCategoryTranslationRow → BankCategoryTranslation ─────────────────

    /// <summary>
    /// Maps a BankCategoryTranslationRow (Postgrest DTO) to a BankCategoryTranslation domain record.
    /// </summary>
    public static BankCategoryTranslation ToDomain(this BankCategoryTranslationRow row)
    {
        return new BankCategoryTranslation(
            row.BankCategory,
            row.BankSubcategory,
            row.ResolvedCategoryName,
            row.ResolvedSubcategoryName);
    }

    // ── TransactionRow ↔ Transaction — handled by TransactionRow.ToDomain() ──
    // and TransactionRow.FromDomain() / FromDomainForInsert() instance/static methods.
    // No extension methods needed — the DTO class has its own mapping logic.
}

using SauronSheet.Application.Features.Transactions.DTOs;

namespace SauronSheet.Frontend.Helpers;

public sealed record TransactionCategoryDisplay(
    string PrimaryText,
    string? SecondaryText,
    bool IsUncategorized,
    bool UsesRawCategoryFallback);

/// <summary>
/// Builds the display model for a transaction's category/subcategory.
///
/// Priority rules (spec DH-1):
///   - UserOverride  → show CategoryName  (user's manual choice is always respected)
///   - BankCategory != null (AutoMatched / RawOnly / any non-override source)
///                   → show BankCategory  (raw PDF literal is canonical for imported txns)
///   - else          → show CategoryName  (Legacy transactions without bank data)
///   - none available → "Uncategorized"
/// </summary>
public static class TransactionCategoryDisplayHelper
{
    private const string UncategorizedLabel = "Uncategorized";
    private const string UserOverrideSource = "UserOverride";

    public static TransactionCategoryDisplay Build(TransactionDto transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        string? resolvedCategory = NormalizeDisplayPart(transaction.CategoryName);
        string? bankCategory = NormalizeDisplayPart(transaction.BankCategory);
        string? resolvedSubcategory = NormalizeDisplayPart(transaction.SubcategoryName);
        string? bankSubcategory = NormalizeDisplayPart(transaction.BankSubcategory);

        bool isUserOverride = string.Equals(
            transaction.CategorySource, UserOverrideSource, StringComparison.Ordinal);

        // DH-1c: UserOverride → always show the user's chosen category name
        // DH-1a/1b: non-override with raw PDF data → show BankCategory literal
        // DH-1d: legacy / no bank data → fall back to resolved name
        string primaryText;
        if (isUserOverride)
        {
            primaryText = resolvedCategory ?? UncategorizedLabel;
        }
        else if (bankCategory is not null)
        {
            primaryText = bankCategory;
        }
        else
        {
            primaryText = resolvedCategory ?? UncategorizedLabel;
        }

        string? secondaryText = resolvedSubcategory ?? bankSubcategory;

        bool isUncategorized = string.Equals(primaryText, UncategorizedLabel, StringComparison.Ordinal);

        // UsesRawCategoryFallback: true when the displayed primary text IS the raw PDF value
        // (i.e. non-UserOverride and BankCategory is what's shown)
        bool usesRawCategoryFallback = !isUserOverride && bankCategory is not null;

        return new TransactionCategoryDisplay(primaryText, secondaryText, isUncategorized, usesRawCategoryFallback);
    }

    public static CategoryBadgeDisplay BuildBadge(TransactionDto transaction)
    {
        TransactionCategoryDisplay display = Build(transaction);
        return new CategoryBadgeDisplay(
            display.PrimaryText,
            display.SecondaryText,
            display.IsUncategorized,
            display.UsesRawCategoryFallback);
    }

    private static string? NormalizeDisplayPart(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}


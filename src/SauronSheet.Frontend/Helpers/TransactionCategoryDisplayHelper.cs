using SauronSheet.Application.Features.Transactions.DTOs;

namespace SauronSheet.Frontend.Helpers;

public sealed record TransactionCategoryDisplay(
    string PrimaryText,
    string? SecondaryText,
    bool IsUncategorized,
    bool UsesRawCategoryFallback);

public static class TransactionCategoryDisplayHelper
{
    private const string UncategorizedLabel = "Uncategorized";

    public static TransactionCategoryDisplay Build(TransactionDto transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        string? resolvedCategory = NormalizeDisplayPart(transaction.CategoryName);
        string? bankCategory = NormalizeDisplayPart(transaction.BankCategory);
        string? resolvedSubcategory = NormalizeDisplayPart(transaction.SubcategoryName);
        string? bankSubcategory = NormalizeDisplayPart(transaction.BankSubcategory);

        string primaryText = resolvedCategory
            ?? bankCategory
            ?? UncategorizedLabel;

        string? secondaryText = resolvedSubcategory
            ?? bankSubcategory;

        bool isUncategorized = string.Equals(primaryText, UncategorizedLabel, StringComparison.Ordinal);
        bool usesRawCategoryFallback = resolvedCategory is null && bankCategory is not null;

        return new TransactionCategoryDisplay(primaryText, secondaryText, isUncategorized, usesRawCategoryFallback);
    }

    private static string? NormalizeDisplayPart(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Frontend.Helpers;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Transactions;

/// <summary>
/// Tests for spec DH-1: TransactionCategoryDisplayHelper MUST be CategorySource-aware.
///
/// DH-1a: AutoMatched + BankCategory set → shows BankCategory (raw PDF value)
/// DH-1b: RawOnly   + BankCategory set → shows BankCategory (raw PDF value, even if CategoryName exists)
/// DH-1c: UserOverride                 → shows CategoryName (manual override respected)
/// DH-1d: Legacy + BankCategory=null  → shows CategoryName (no raw PDF data available)
///
/// Contract: CategorySource drives priority; raw PDF literals take precedence over
/// resolved names unless the user explicitly overrode the category (UserOverride).
/// </summary>
public class TransactionCategoryDisplayHelperSourceAwareTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly DateTime Date = new(2026, 5, 1);
    private static readonly DateTime CreatedAt = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    // ─── DH-1a: AutoMatched + BankCategory set ───────────────────────────────────────

    /// <summary>
    /// DH-1a: AutoMatched transaction with BankCategory set MUST display BankCategory,
    /// NOT the resolved CategoryName. The raw PDF value is the canonical display for imports.
    /// </summary>
    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_DH1a_AutoMatched_WithBankCategory_ShowsBankCategoryNotCategoryName()
    {
        // Arrange — AutoMatched: resolver found/created "Alimentación" for the raw "Compras"
        TransactionDto dto = MakeDto(
            categorySource: "AutoMatched",
            categoryName: "Alimentación",   // resolved name — should NOT be shown
            bankCategory: "Compras",        // raw PDF literal — MUST be shown
            subcategoryName: "Supermercado",
            bankSubcategory: "Supermercados y alimentación");

        // Act
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(dto);

        // Assert — DH-1a: raw PDF value shown, not resolved name
        Assert.Equal("Compras", result.PrimaryText);
        Assert.False(result.IsUncategorized);
    }

    // ─── DH-1b: RawOnly + BankCategory set (even when CategoryName also set) ─────────

    /// <summary>
    /// DH-1b: RawOnly transaction with BankCategory set MUST display BankCategory.
    /// This is the case where the category exists in the PDF but no matching user category was found.
    /// Even if CategoryName is somehow set, BankCategory takes priority.
    /// </summary>
    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_DH1b_RawOnly_WithBankCategoryAndCategoryName_ShowsBankCategory()
    {
        // Arrange — RawOnly: bank category captured but no user category resolved
        TransactionDto dto = MakeDto(
            categorySource: "RawOnly",
            categoryName: "SomeResolvedName",  // should NOT be shown — RawOnly + BankCat → BankCat
            bankCategory: "Ocio y tiempo libre",
            subcategoryName: null,
            bankSubcategory: null);

        // Act
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(dto);

        // Assert — DH-1b: BankCategory shown, not CategoryName
        Assert.Equal("Ocio y tiempo libre", result.PrimaryText);
        Assert.False(result.IsUncategorized);
    }

    // ─── DH-1c: UserOverride → CategoryName ──────────────────────────────────────────

    /// <summary>
    /// DH-1c: UserOverride transaction MUST show CategoryName (the user's choice),
    /// even when BankCategory is also set. The user's override always wins.
    /// </summary>
    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_DH1c_UserOverride_ShowsCategoryNameOverBankCategory()
    {
        // Arrange — UserOverride: user explicitly categorised this transaction
        TransactionDto dto = MakeDto(
            categorySource: "UserOverride",
            categoryName: "Mi Categoría Personal", // user's choice — MUST be shown
            bankCategory: "Compras",               // raw PDF — must NOT override user's choice
            subcategoryName: "Subcategoría personal",
            bankSubcategory: "Ropa y complementos");

        // Act
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(dto);

        // Assert — DH-1c: user override respected; raw PDF category NOT shown
        Assert.Equal("Mi Categoría Personal", result.PrimaryText);
        Assert.Equal("Subcategoría personal", result.SecondaryText);
        Assert.False(result.IsUncategorized);
        Assert.False(result.UsesRawCategoryFallback);
    }

    // ─── DH-1c triangulation: UserOverride with null CategoryName ────────────────────

    /// <summary>
    /// DH-1c triangulation: UserOverride with no CategoryName → Uncategorized.
    /// BankCategory MUST NOT leak through even in this edge case.
    /// </summary>
    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_DH1c_UserOverride_WithNullCategoryName_ShowsUncategorized()
    {
        // Arrange — cleared UserOverride: user removed category assignment
        TransactionDto dto = MakeDto(
            categorySource: "UserOverride",
            categoryName: null,         // cleared
            bankCategory: "Compras",   // MUST NOT show through
            subcategoryName: null,
            bankSubcategory: null);

        // Act
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(dto);

        // Assert — DH-1c edge: no user category → Uncategorized, NOT BankCategory
        Assert.Equal("Uncategorized", result.PrimaryText);
        Assert.True(result.IsUncategorized);
        Assert.False(result.UsesRawCategoryFallback);
    }

    // ─── DH-1d: Legacy + BankCategory=null → CategoryName ───────────────────────────

    /// <summary>
    /// DH-1d: Legacy transaction without BankCategory MUST show CategoryName.
    /// Legacy transactions pre-date the PDF import feature; they have no raw bank values.
    /// </summary>
    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_DH1d_Legacy_WithNoBankCategory_ShowsCategoryName()
    {
        // Arrange — Legacy: transaction from before PDF import feature
        TransactionDto dto = MakeDto(
            categorySource: "Legacy",
            categoryName: "Alimentación",
            bankCategory: null,        // legacy has no bank category
            subcategoryName: "Supermercado",
            bankSubcategory: null);

        // Act
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(dto);

        // Assert — DH-1d: CategoryName shown for legacy transactions
        Assert.Equal("Alimentación", result.PrimaryText);
        Assert.Equal("Supermercado", result.SecondaryText);
        Assert.False(result.IsUncategorized);
        Assert.False(result.UsesRawCategoryFallback);
    }

    // ─── UsesRawCategoryFallback semantics ───────────────────────────────────────────

    /// <summary>
    /// UsesRawCategoryFallback MUST be true when BankCategory is the displayed primary text.
    /// This signal lets the UI render raw category values with distinct styling.
    /// </summary>
    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_DH1a_AutoMatched_UsesRawCategoryFallbackIsTrue()
    {
        TransactionDto dto = MakeDto(
            categorySource: "AutoMatched",
            categoryName: "Alimentación",
            bankCategory: "Compras",
            subcategoryName: null,
            bankSubcategory: null);

        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(dto);

        // DH-1a: showing raw bank category → flag is true
        Assert.True(result.UsesRawCategoryFallback);
    }

    /// <summary>
    /// UsesRawCategoryFallback MUST be false for UserOverride (showing CategoryName).
    /// </summary>
    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_DH1c_UserOverride_UsesRawCategoryFallbackIsFalse()
    {
        TransactionDto dto = MakeDto(
            categorySource: "UserOverride",
            categoryName: "My Category",
            bankCategory: "Compras",
            subcategoryName: null,
            bankSubcategory: null);

        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(dto);

        // DH-1c: showing CategoryName (UserOverride) → flag is false
        Assert.False(result.UsesRawCategoryFallback);
    }

    // ─── Helper ───────────────────────────────────────────────────────────────────────

    private static TransactionDto MakeDto(
        string categorySource,
        string? categoryName,
        string? bankCategory,
        string? subcategoryName,
        string? bankSubcategory) =>
        new TransactionDto(
            Id: Id,
            Amount: -100m,
            Currency: "EUR",
            Date: Date,
            Description: "Test transaction",
            CategoryId: null,
            CategoryName: categoryName,
            ImportedFrom: "test.pdf",
            CreatedAt: CreatedAt,
            BankCategory: bankCategory,
            BankSubcategory: bankSubcategory,
            SubcategoryId: null,
            SubcategoryName: subcategoryName,
            CategorySource: categorySource);
}

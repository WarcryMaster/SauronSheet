using MediatR;
using Moq;
using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Frontend.Helpers;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Transactions;

public class IndexModelTests
{
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly DateTime TransactionDate = new(2026, 5, 25);
    private static readonly DateTime CreatedAt = new(2026, 5, 25, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryAndSubcategory_ReturnsResolvedValues()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: "Food",
            subcategoryName: "Dining Out",
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Food", result.PrimaryText);
        Assert.Equal("Dining Out", result.SecondaryText);
        Assert.False(result.IsUncategorized);
        Assert.False(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryWithoutSubcategory_UsesBankSubcategoryFallbackIndependently()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: "Food",
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Food", result.PrimaryText);
        Assert.Equal("Restaurantes", result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategory_UsesRawBankCategoryAndSubcategory()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: "Ropa y complementos");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Equal("Ropa y complementos", result.SecondaryText);
        Assert.False(result.IsUncategorized);
        Assert.True(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategoryOrBankSubcategory_UsesRawBankCategoryOnly()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: null);
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Null(result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategoryButResolvedSubcategory_UsesResolvedSubcategory()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: "Dining Out",
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes");

        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Equal("Dining Out", result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoUsefulPrimaryCategoryInformation_ReturnsUncategorizedAndBankSubcategoryWhenAvailable()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "   ",
            bankSubcategory: " Other ");

        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Uncategorized", result.PrimaryText);
        Assert.Equal("Other", result.SecondaryText);
        Assert.True(result.IsUncategorized);
        Assert.False(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoUsefulCategoryInformation_ReturnsUncategorized()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "   ",
            bankSubcategory: " ");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Uncategorized", result.PrimaryText);
        Assert.Null(result.SecondaryText);
        Assert.True(result.IsUncategorized);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryWhitespace_FallsBackToTrimmedBankValues()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: "   ",
            subcategoryName: "Ignored",
            bankCategory: " Compras ",
            bankSubcategory: " Ropa ");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.Equal("Compras", result.PrimaryText);
        Assert.Equal("Ignored", result.SecondaryText);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoUsefulCategoryInformation_MarksUncategorized()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: null,
            bankSubcategory: null);
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.True(result.IsUncategorized);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedOrRawCategoryExists_DoesNotMarkUncategorized()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: null);
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.False(result.IsUncategorized);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_NoResolvedCategoryButBankCategoryExists_MarksRawCategoryFallback()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: null,
            subcategoryName: null,
            bankCategory: "Compras",
            bankSubcategory: "Ropa y complementos");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.True(result.UsesRawCategoryFallback);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void Build_ResolvedCategoryExists_DoesNotMarkRawCategoryFallback()
    {
        TransactionDto transaction = CreateTransaction(
            categoryName: "Food",
            subcategoryName: "Dining Out",
            bankCategory: "Compras",
            bankSubcategory: "Restaurantes");
        TransactionCategoryDisplay result = TransactionCategoryDisplayHelper.Build(transaction);

        Assert.False(result.UsesRawCategoryFallback);
    }

    private static TransactionDto CreateTransaction(
        string? categoryName,
        string? subcategoryName,
        string? bankCategory,
        string? bankSubcategory)
    {
        return new TransactionDto(
            Id: TransactionId,
            Amount: -25.50m,
            Currency: "EUR",
            Date: TransactionDate,
            Description: "Test transaction",
            CategoryId: null,
            CategoryName: categoryName,
            ImportedFrom: "statement.pdf",
            CreatedAt: CreatedAt,
            BankCategory: bankCategory,
            BankSubcategory: bankSubcategory,
            SubcategoryId: null,
            SubcategoryName: subcategoryName,
            CategorySource: "RawOnly");
    }
}

namespace SauronSheet.Domain.Tests.Entities;

using System;
using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class TransactionTests
{
    private readonly TransactionId _transactionId = new(Guid.NewGuid());
    private readonly UserId _userId = new("user-123");
    private readonly Money _amount = new(100.50m, "EUR");
    private readonly DateTime _date = new(2024, 1, 15);
    private readonly CategoryId _categoryId = CategoryId.New();
    private readonly SubcategoryId _subcategoryId = SubcategoryId.New();

    [Fact]
    public void Constructor_WithMinimalParams_BackwardCompatible()
    {
        // Arrange & Act - Same signature as before the change
        var transaction = new Transaction(
            _transactionId,
            _userId,
            _amount,
            _date,
            "Test transaction");

        // Assert - Backward compatible: old behavior preserved
        Assert.NotNull(transaction);
        Assert.Equal(_transactionId, transaction.Id);
        Assert.Equal(_userId, transaction.UserId);
        Assert.Equal(_amount, transaction.Amount);
        Assert.Equal(_date, transaction.Date);
        Assert.Equal("Test transaction", transaction.Description);
        Assert.Null(transaction.CategoryId);
        Assert.Null(transaction.ImportedFrom);

        // New defaults
        Assert.Null(transaction.BankCategory);
        Assert.Null(transaction.BankSubcategory);
        Assert.Null(transaction.SubcategoryId);
        Assert.Equal(CategorySource.Legacy, transaction.CategorySource);
    }

    [Fact]
    public void Constructor_WithCategoryIdAndImportedFrom_BackwardCompatible()
    {
        // Arrange & Act - Original optional params still work
        var transaction = new Transaction(
            _transactionId,
            _userId,
            _amount,
            _date,
            "Test transaction",
            _categoryId,
            "PDF");

        // Assert
        Assert.Equal(_categoryId, transaction.CategoryId);
        Assert.Equal("PDF", transaction.ImportedFrom);
        Assert.Null(transaction.BankCategory);
        Assert.Null(transaction.BankSubcategory);
        Assert.Equal(CategorySource.Legacy, transaction.CategorySource);
    }

    [Fact]
    public void Constructor_WithAllNewParams_SetsNewProperties()
    {
        // Arrange & Act - New constructor with all params
        var transaction = new Transaction(
            _transactionId,
            _userId,
            _amount,
            _date,
            "Test transaction",
            _categoryId,
            "PDF",
            "Bank Category",
            "Bank Subcategory",
            _subcategoryId,
            CategorySource.AutoMatched);

        // Assert
        Assert.Equal("Bank Category", transaction.BankCategory);
        Assert.Equal("Bank Subcategory", transaction.BankSubcategory);
        Assert.Equal(_subcategoryId, transaction.SubcategoryId);
        Assert.Equal(CategorySource.AutoMatched, transaction.CategorySource);
        Assert.Equal(_categoryId, transaction.CategoryId);
    }

    [Fact]
    public void Constructor_WithOnlyBankValues_DefaultsSourceToLegacy()
    {
        // Arrange & Act - Only bank values, no category assignment
        var transaction = new Transaction(
            _transactionId,
            _userId,
            _amount,
            _date,
            "Test",
            null,
            null,
            "Bank Category",
            "Bank Subcategory");

        // Assert
        Assert.Equal("Bank Category", transaction.BankCategory);
        Assert.Equal("Bank Subcategory", transaction.BankSubcategory);
        Assert.Null(transaction.SubcategoryId);
        Assert.Null(transaction.CategoryId);
        Assert.Equal(CategorySource.Legacy, transaction.CategorySource);
    }

    [Fact]
    public void Categorize_WithCategoryId_SetsUserOverride()
    {
        // Arrange
        var transaction = new Transaction(
            _transactionId, _userId, _amount, _date, "Test", null, null,
            "Bank Cat", null, null, CategorySource.RawOnly);

        Assert.Equal(CategorySource.RawOnly, transaction.CategorySource);

        // Act
        transaction.Categorize(_categoryId);

        // Assert
        Assert.Equal(_categoryId, transaction.CategoryId);
        Assert.Equal(CategorySource.UserOverride, transaction.CategorySource);
    }

    [Fact]
    public void Categorize_WithNullCategoryId_PreservesSource()
    {
        // Arrange
        var transaction = new Transaction(
            _transactionId, _userId, _amount, _date, "Test", null, null,
            "Bank Cat", null, null, CategorySource.RawOnly);

        Assert.Equal(CategorySource.RawOnly, transaction.CategorySource);

        // Act - Uncategorize
        transaction.Categorize((CategoryId?)null);

        // Assert - Source should be preserved (not overwritten)
        Assert.Null(transaction.CategoryId);
        Assert.Equal(CategorySource.RawOnly, transaction.CategorySource);
    }

    [Fact]
    public void Categorize_NullToNull_PreservesLegacySource()
    {
        // Arrange - Transaction created with no category, default source
        var transaction = new Transaction(
            _transactionId, _userId, _amount, _date, "Test");

        Assert.Equal(CategorySource.Legacy, transaction.CategorySource);

        // Act - Already null, categorizing with null again
        transaction.Categorize((CategoryId?)null);

        // Assert
        Assert.Null(transaction.CategoryId);
        Assert.Equal(CategorySource.Legacy, transaction.CategorySource);
    }

    [Fact]
    public void Categorize_NewOverload_SetsAllProperties()
    {
        // Arrange
        var transaction = new Transaction(
            _transactionId, _userId, _amount, _date, "Test");

        // Act
        transaction.Categorize(_categoryId, _subcategoryId, CategorySource.AutoMatched);

        // Assert
        Assert.Equal(_categoryId, transaction.CategoryId);
        Assert.Equal(_subcategoryId, transaction.SubcategoryId);
        Assert.Equal(CategorySource.AutoMatched, transaction.CategorySource);
    }

    [Fact]
    public void Categorize_NewOverload_CanSetRawOnlySource()
    {
        // Arrange
        var transaction = new Transaction(
            _transactionId, _userId, _amount, _date, "Test", _categoryId);

        // Act - Resolution service sets RawOnly when no match found
        transaction.Categorize(null, null, CategorySource.RawOnly);

        // Assert
        Assert.Null(transaction.CategoryId);
        Assert.Null(transaction.SubcategoryId);
        Assert.Equal(CategorySource.RawOnly, transaction.CategorySource);
    }

    [Fact]
    public void Categorize_UpdatesUpdatedAt()
    {
        // Arrange
        var transaction = new Transaction(
            _transactionId, _userId, _amount, _date, "Test");
        Assert.Null(transaction.UpdatedAt);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        transaction.Categorize(_categoryId);

        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(transaction.UpdatedAt);
        Assert.True(transaction.UpdatedAt >= beforeUpdate && transaction.UpdatedAt <= afterUpdate);
    }
}

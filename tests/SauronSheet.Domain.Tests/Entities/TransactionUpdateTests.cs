namespace SauronSheet.Domain.Tests.Entities;

using System;
using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

[Trait("Category", "Domain")]
public class TransactionUpdateTests
{
    private readonly TransactionId _transactionId = new(Guid.NewGuid());
    private readonly UserId _userId = new("user-123");
    private readonly Money _initialAmount = new(100.50m, "EUR");
    private readonly DateTime _initialDate = new(2024, 1, 15);
    private readonly CategoryId _categoryId = CategoryId.New();
    private readonly SubcategoryId _subcategoryId = SubcategoryId.New();

    private Transaction CreateTransaction()
    {
        return new Transaction(
            _transactionId,
            _userId,
            _initialAmount,
            _initialDate,
            "Initial description",
            _categoryId,
            "bank.xlsx",
            "Food",
            "Groceries",
            _subcategoryId,
            CategorySource.AutoMatched,
            1500.00m);
    }

    [Fact]
    public void Update_AllFields_SetsAllMutableProperties()
    {
        // Arrange
        Transaction transaction = CreateTransaction();
        Money newAmount = new(250.75m, "EUR");
        DateTime newDate = new(2024, 6, 20);
        string newDescription = "Updated description";
        CategoryId newCategoryId = CategoryId.New();
        SubcategoryId newSubcategoryId = SubcategoryId.New();

        // Act
        transaction.Update(newAmount, newDate, newDescription, newCategoryId, newSubcategoryId, CategorySource.UserOverride);

        // Assert - mutable fields updated
        Assert.Equal(newAmount, transaction.Amount);
        Assert.Equal(newDescription, transaction.Description);
        Assert.Equal(newCategoryId, transaction.CategoryId);
        Assert.Equal(newSubcategoryId, transaction.SubcategoryId);
        Assert.Equal(CategorySource.UserOverride, transaction.CategorySource);
        Assert.NotNull(transaction.UpdatedAt);

        // Assert - immutable fields preserved
        Assert.Equal(_transactionId, transaction.Id);
        Assert.Equal(_userId, transaction.UserId);
    }

    [Fact]
    public void Update_NullDescription_ThrowsDomainException()
    {
        // Arrange
        Transaction transaction = CreateTransaction();
        Money amount = new(100m, "EUR");
        DateTime date = new(2024, 3, 1);

        // Act & Assert
        DomainException exception = Assert.Throws<DomainException>(
            () => transaction.Update(amount, date, null!, null, null, CategorySource.Legacy));
        Assert.Equal("Description is required.", exception.Message);
    }

    [Fact]
    public void Update_EmptyDescription_ThrowsDomainException()
    {
        // Arrange
        Transaction transaction = CreateTransaction();
        Money amount = new(100m, "EUR");
        DateTime date = new(2024, 3, 1);

        // Act & Assert
        DomainException exception = Assert.Throws<DomainException>(
            () => transaction.Update(amount, date, "   ", null, null, CategorySource.Legacy));
        Assert.Equal("Description is required.", exception.Message);
    }

    [Fact]
    public void Update_DescriptionTooLong_ThrowsDomainException()
    {
        // Arrange
        Transaction transaction = CreateTransaction();
        Money amount = new(100m, "EUR");
        DateTime date = new(2024, 3, 1);
        string longDescription = new string('A', 501);

        // Act & Assert
        DomainException exception = Assert.Throws<DomainException>(
            () => transaction.Update(amount, date, longDescription, null, null, CategorySource.Legacy));
        Assert.Equal("Description must be 500 characters or less.", exception.Message);
    }

    [Fact]
    public void Update_NormalizesDateToUtc()
    {
        // Arrange
        Transaction transaction = CreateTransaction();
        Money amount = new(100m, "EUR");
        DateTime unspecifiedDate = new(2024, 6, 15, 10, 30, 0, DateTimeKind.Unspecified);
        Money validAmount = new(100m, "EUR");

        // Act
        transaction.Update(validAmount, unspecifiedDate, "Test", null, null, CategorySource.Legacy);

        // Assert
        Assert.Equal(DateTimeKind.Utc, transaction.Date.Kind);
    }

    [Fact]
    public void Update_PreservesImportMetadata()
    {
        // Arrange
        Transaction transaction = CreateTransaction();
        Assert.Equal("bank.xlsx", transaction.ImportedFrom);
        Assert.Equal("Food", transaction.BankCategory);
        Assert.Equal("Groceries", transaction.BankSubcategory);
        Assert.Equal(1500.00m, transaction.Balance);

        Money newAmount = new(999.99m, "EUR");
        DateTime newDate = new(2025, 1, 1);

        // Act
        transaction.Update(newAmount, newDate, "Changed", null, null, CategorySource.Legacy);

        // Assert - import metadata untouched
        Assert.Equal("bank.xlsx", transaction.ImportedFrom);
        Assert.Equal("Food", transaction.BankCategory);
        Assert.Equal("Groceries", transaction.BankSubcategory);
        Assert.Equal(1500.00m, transaction.Balance);
    }
}

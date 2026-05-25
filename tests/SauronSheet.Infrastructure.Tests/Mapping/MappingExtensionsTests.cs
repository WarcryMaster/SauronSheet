namespace SauronSheet.Infrastructure.Tests.Mapping;

using System;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Persistence;
using Infrastructure.Mapping;
using Xunit;

/// <summary>
/// Tests for MappingExtensions that convert between Postgrest DTOs and Domain objects.
/// These are pure functions — no mocking needed.
/// </summary>
[Trait("Category", "Infrastructure")]
public class MappingExtensionsTests
{
    // ── SubcategoryRow → Subcategory ──────────────────────────────────────────

    [Fact]
    public void SubcategoryRow_ToDomain_MapsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "auth0|user123";
        var categoryId = Guid.NewGuid();
        var row = new SubcategoryRow
        {
            Id = id.ToString(),
            UserId = userId,
            CategoryId = categoryId.ToString(),
            Name = "Ropa y complementos",
            IsAutoCreated = true
        };

        // Act
        var domain = MappingExtensions.ToDomain(row);

        // Assert
        Assert.NotNull(domain);
        Assert.Equal(id, domain!.Id.Value);
        Assert.Equal(userId, domain.UserId!.Value);
        Assert.Equal(categoryId, domain.CategoryId.Value);
        Assert.Equal("Ropa y complementos", domain.Name.Value);
        Assert.True(domain.IsAutoCreated);
    }

    [Fact]
    public void SubcategoryRow_ToDomain_WithNullUserId_ProducesNullUserId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var row = new SubcategoryRow
        {
            Id = id.ToString(),
            UserId = null,
            CategoryId = categoryId.ToString(),
            Name = "System subcategory",
            IsAutoCreated = false
        };

        // Act
        var domain = MappingExtensions.ToDomain(row);

        // Assert
        Assert.NotNull(domain);
        Assert.Null(domain!.UserId);
        Assert.Equal("System subcategory", domain.Name.Value);
    }

    [Fact]
    public void Subcategory_ToSubcategoryRow_MapsAllProperties()
    {
        // Arrange
        var id = new SubcategoryId(Guid.NewGuid());
        var userId = new UserId("auth0|user456");
        var categoryId = new CategoryId(Guid.NewGuid());
        var name = SubcategoryName.Create("Alimentación");
        var subcategory = new Subcategory(id, userId, categoryId, name, false);

        // Act
        var row = MappingExtensions.FromDomain(subcategory, "alimentacion");

        // Assert
        Assert.Equal(id.Value.ToString(), row.Id);
        Assert.Equal(userId.Value, row.UserId);
        Assert.Equal(categoryId.Value.ToString(), row.CategoryId);
        Assert.Equal("Alimentación", row.Name);
        Assert.Equal("alimentacion", row.NormalizedName);
        Assert.False(row.IsAutoCreated);
    }

    // ── BankCategoryTranslationRow → BankCategoryTranslation ──────────────────

    [Fact]
    public void BankCategoryTranslationRow_ToDomain_MapsAllProperties()
    {
        // Arrange
        var row = new BankCategoryTranslationRow
        {
            Id = Guid.NewGuid().ToString(),
            BankCategory = "Aliment.",
            BankSubcategory = null,
            ResolvedCategoryName = "Alimentación",
            ResolvedSubcategoryName = null
        };

        // Act
        var translation = row.ToDomain();

        // Assert
        Assert.NotNull(translation);
        Assert.Equal("Aliment.", translation!.BankCategory);
        Assert.Null(translation.BankSubcategory);
        Assert.Equal("Alimentación", translation.ResolvedCategoryName);
        Assert.Null(translation.ResolvedSubcategoryName);
    }

    [Fact]
    public void BankCategoryTranslationRow_ToDomain_WithSubcategory_MapsCorrectly()
    {
        // Arrange
        var row = new BankCategoryTranslationRow
        {
            Id = Guid.NewGuid().ToString(),
            BankCategory = "Compras",
            BankSubcategory = "Ropa",
            ResolvedCategoryName = "Shopping",
            ResolvedSubcategoryName = "Clothing"
        };

        // Act
        var translation = row.ToDomain();

        // Assert
        Assert.NotNull(translation);
        Assert.Equal("Compras", translation!.BankCategory);
        Assert.Equal("Ropa", translation.BankSubcategory);
        Assert.Equal("Shopping", translation.ResolvedCategoryName);
        Assert.Equal("Clothing", translation.ResolvedSubcategoryName);
    }

    // ── TransactionRow → Transaction new field mappings ───────────────────────

    [Fact]
    public void TransactionRow_ToDomain_MapsBankCategoryFields()
    {
        // Arrange
        var row = new TransactionRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "auth0|user1",
            Amount = 100m,
            Currency = "EUR",
            Date = new DateTime(2024, 1, 15),
            Description = "Test transaction",
            CategoryId = null,
            ImportedFrom = null,
            BankCategory = "Compras",
            BankSubcategory = "Ropa",
            SubcategoryId = null,
            CategorySourceColumn = "AutoMatched"
        };

        // Act
        var transaction = row.ToDomain();

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal("Compras", transaction!.BankCategory);
        Assert.Equal("Ropa", transaction.BankSubcategory);
        Assert.Null(transaction.SubcategoryId);
        Assert.Equal(CategorySource.AutoMatched, transaction.CategorySource);
    }

    [Fact]
    public void TransactionRow_ToDomain_WithNullBankFields_SetsLegacySource()
    {
        // Arrange
        var row = new TransactionRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "auth0|user1",
            Amount = 50m,
            Currency = "EUR",
            Date = new DateTime(2024, 1, 15),
            Description = "Legacy transaction",
            CategoryId = Guid.NewGuid().ToString(),
            ImportedFrom = null,
            BankCategory = null,
            BankSubcategory = null,
            SubcategoryId = null,
            CategorySourceColumn = null
        };

        // Act
        var transaction = row.ToDomain();

        // Assert
        Assert.NotNull(transaction);
        Assert.Null(transaction!.BankCategory);
        Assert.Null(transaction.BankSubcategory);
        Assert.Equal(CategorySource.Legacy, transaction.CategorySource);
    }

    [Fact]
    public void TransactionRow_ToDomain_WithSubcategoryId_ParsesCorrectly()
    {
        // Arrange
        var subId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var row = new TransactionRow
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "auth0|user1",
            Amount = 75m,
            Currency = "EUR",
            Date = new DateTime(2024, 3, 10),
            Description = "With subcategory",
            CategoryId = catId.ToString(),
            ImportedFrom = null,
            BankCategory = "Compras",
            BankSubcategory = "Ropa y complementos",
            SubcategoryId = subId.ToString(),
            CategorySourceColumn = "AutoMatched"
        };

        // Act
        var transaction = row.ToDomain();

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(subId, transaction!.SubcategoryId!.Value);
        Assert.Equal(catId, transaction.CategoryId!.Value);
        Assert.Equal(CategorySource.AutoMatched, transaction.CategorySource);
    }

    [Fact]
    public void Transaction_ToTransactionRow_MapsNewFieldsBack()
    {
        // Arrange
        var subId = new SubcategoryId(Guid.NewGuid());
        var catId = new CategoryId(Guid.NewGuid());
        var transaction = new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("auth0|user1"),
            new Money(100m, "EUR"),
            new DateTime(2024, 1, 15),
            "Test transaction",
            catId,
            "ING",
            "Compras",
            "Ropa",
            subId,
            CategorySource.AutoMatched);

        // Act
        var row = TransactionRow.FromDomain(transaction);

        // Assert
        Assert.Equal("Compras", row.BankCategory);
        Assert.Equal("Ropa", row.BankSubcategory);
        Assert.Equal(subId.Value.ToString(), row.SubcategoryId);
        Assert.Equal("AutoMatched", row.CategorySourceColumn);
    }
}

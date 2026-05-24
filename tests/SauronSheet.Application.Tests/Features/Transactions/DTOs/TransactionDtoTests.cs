using Xunit;
using System.Text.Json;
using SauronSheet.Application.Features.Transactions.DTOs;

namespace SauronSheet.Application.Tests.Features.Transactions.DTOs;

/// <summary>
/// Tests for TransactionDto fields including the new BankCategory, BankSubcategory,
/// SubcategoryId, SubcategoryName, and CategorySource properties.
/// Phase 3 (bank-category-resolution): DTO extension for resolution data.
/// </summary>
[Trait("Category", "Application")]
public class TransactionDtoTests
{
    [Fact]
    public void Create_WithNewFields_PopulatesAllProperties()
    {
        // Arrange & Act
        var dto = new TransactionDto(
            Id: Guid.NewGuid(),
            Amount: 100.50m,
            Currency: "EUR",
            Date: new DateTime(2024, 1, 15),
            Description: "Test transaction",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Groceries",
            ImportedFrom: "test.pdf",
            CreatedAt: DateTime.UtcNow,
            BankCategory: "Compras",
            BankSubcategory: "Ropa",
            SubcategoryId: Guid.NewGuid().ToString(),
            SubcategoryName: "Ropa y complementos",
            CategorySource: "AutoMatched");

        // Assert
        Assert.Equal("Compras", dto.BankCategory);
        Assert.Equal("Ropa", dto.BankSubcategory);
        Assert.NotNull(dto.SubcategoryId);
        Assert.Equal("Ropa y complementos", dto.SubcategoryName);
        Assert.Equal("AutoMatched", dto.CategorySource);
    }

    [Fact]
    public void Create_WithoutNewFields_UsesDefaults()
    {
        // Arrange & Act
        var dto = new TransactionDto(
            Id: Guid.NewGuid(),
            Amount: 50.00m,
            Currency: "EUR",
            Date: new DateTime(2024, 2, 1),
            Description: "Legacy transaction",
            CategoryId: null,
            CategoryName: null,
            ImportedFrom: null,
            CreatedAt: DateTime.UtcNow);

        // Assert — new fields default to null/Legacy
        Assert.Null(dto.BankCategory);
        Assert.Null(dto.BankSubcategory);
        Assert.Null(dto.SubcategoryId);
        Assert.Null(dto.SubcategoryName);
        Assert.Equal("Legacy", dto.CategorySource);
    }

    [Fact]
    public void Serialization_ToJsonAndBack_PreservesNewFields()
    {
        // Arrange
        var dto = new TransactionDto(
            Id: Guid.NewGuid(),
            Amount: 75.00m,
            Currency: "EUR",
            Date: new DateTime(2024, 3, 1),
            Description: "Serialization test",
            CategoryId: Guid.NewGuid(),
            CategoryName: "Transport",
            ImportedFrom: "statement.pdf",
            CreatedAt: DateTime.UtcNow,
            BankCategory: "Transporte",
            BankSubcategory: null,
            SubcategoryId: null,
            SubcategoryName: null,
            CategorySource: "RawOnly");

        // Act
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(dto, options);
        var deserialized = JsonSerializer.Deserialize<TransactionDto>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(dto.BankCategory, deserialized.BankCategory);
        Assert.Equal(dto.CategorySource, deserialized.CategorySource);
        Assert.Equal(dto.Amount, deserialized.Amount);
    }
}

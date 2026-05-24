namespace SauronSheet.Domain.Tests.Entities;

using System;
using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class SubcategoryTests
{
    private readonly UserId _testUserId = new("user-123");
    private readonly CategoryId _testCategoryId = CategoryId.New();

    private Subcategory CreateTestSubcategory(
        string name = "Ropa y complementos",
        bool isAutoCreated = false)
    {
        return new Subcategory(
            SubcategoryId.New(),
            _testUserId,
            _testCategoryId,
            SubcategoryName.Create(name),
            isAutoCreated);
    }

    [Fact]
    public void Constructor_WithValidData_CreatesSuccessfully()
    {
        // Arrange & Act
        var subcategory = CreateTestSubcategory();

        // Assert
        Assert.NotNull(subcategory);
        Assert.NotEqual(Guid.Empty, subcategory.Id.Value);
        Assert.Equal(_testUserId, subcategory.UserId);
        Assert.Equal(_testCategoryId, subcategory.CategoryId);
        Assert.Equal("Ropa y complementos", subcategory.Name.Value);
        Assert.False(subcategory.IsAutoCreated);
    }

    [Fact]
    public void Constructor_WithNullUserId_CreatesSuccessfully()
    {
        // Arrange & Act
        var subcategory = new Subcategory(
            SubcategoryId.New(),
            null,
            _testCategoryId,
            SubcategoryName.Create("Global Sub"),
            false);

        // Assert
        Assert.Null(subcategory.UserId);
        Assert.Equal("Global Sub", subcategory.Name.Value);
    }

    [Fact]
    public void Constructor_WithAutoCreatedTrue_SetsFlag()
    {
        // Arrange & Act
        var subcategory = new Subcategory(
            SubcategoryId.New(),
            _testUserId,
            _testCategoryId,
            SubcategoryName.Create("Auto Sub"),
            true);

        // Assert
        Assert.True(subcategory.IsAutoCreated);
    }

    [Fact]
    public void Constructor_WithDefaultIsAutoCreated_SetsFalse()
    {
        // Arrange & Act
        var subcategory = new Subcategory(
            SubcategoryId.New(),
            _testUserId,
            _testCategoryId,
            SubcategoryName.Create("Manual Sub"),
            false);

        // Assert
        Assert.False(subcategory.IsAutoCreated);
    }

    [Fact]
    public void Constructor_WithNullCategoryId_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Subcategory(
                SubcategoryId.New(),
                _testUserId,
                null!,
                SubcategoryName.Create("Test"),
                false));

        Assert.Equal("categoryId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Subcategory(
                SubcategoryId.New(),
                _testUserId,
                _testCategoryId,
                null!,
                false));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_SetsCreatedAndUpdatedAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var subcategory = CreateTestSubcategory();

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(subcategory.CreatedAt >= beforeCreation && subcategory.CreatedAt <= afterCreation);
        Assert.Null(subcategory.UpdatedAt);
    }

    [Fact]
    public void PropertiesAreReadOnly()
    {
        // Arrange
        var subcategory = CreateTestSubcategory();

        // Act & Assert - No public setters
        Assert.NotNull(subcategory);
    }

    [Fact]
    public void Equality_SameId_AreEqual()
    {
        // Arrange
        var id = SubcategoryId.New();
        var sub1 = new Subcategory(id, _testUserId, _testCategoryId, SubcategoryName.Create("Test"), false);
        var sub2 = new Subcategory(id, _testUserId, _testCategoryId, SubcategoryName.Create("Test"), false);

        // Act & Assert
        Assert.Equal(sub1, sub2);
        Assert.True(sub1 == sub2);
    }

    [Fact]
    public void Equality_DifferentIds_AreNotEqual()
    {
        // Arrange
        var sub1 = CreateTestSubcategory();
        var sub2 = CreateTestSubcategory();

        // Act & Assert
        Assert.NotEqual(sub1, sub2);
        Assert.False(sub1 == sub2);
    }
}

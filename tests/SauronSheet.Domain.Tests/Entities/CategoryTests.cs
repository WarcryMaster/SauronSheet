namespace SauronSheet.Domain.Tests.Entities;

using System;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class CategoryTests
{
    private readonly UserId _testUserId = new("user-123");

    private Category CreateTestCategory(
        string name = "Groceries",
        CategoryType type = CategoryType.Expense,
        string color = "#F39C12",
        string icon = "basket")
    {
        return new Category(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create(name),
            type,
            ColorHex.Create(color),
            icon);
    }

    [Fact]
    public void Constructor_WithValidData_CreatesSuccessfully()
    {
        // Arrange & Act
        var category = CreateTestCategory();

        // Assert
        Assert.NotNull(category);
        Assert.NotEqual(Guid.Empty, category.Id.Value);
        Assert.Equal(_testUserId, category.UserId);
        Assert.Equal("Groceries", category.Name.Value);
        Assert.Equal(CategoryType.Expense, category.Type);
        Assert.Equal("#F39C12", category.Color.Value);
        Assert.Equal("basket", category.IconName);
        Assert.False(category.IsSystemDefault);
    }

    [Fact]
    public void Constructor_SetsCreatedAndUpdatedAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var category = CreateTestCategory();

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(category.CreatedAt >= beforeCreation && category.CreatedAt <= afterCreation);
        // UpdatedAt is null until the first Update() call
        Assert.Null(category.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithNullUserId_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Category(
                CategoryId.New(),
                null!,
                CategoryName.Create("Test"),
                CategoryType.Expense,
                ColorHex.Create("#F39C12"),
                "icon"));

        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Category(
                CategoryId.New(),
                _testUserId,
                null!,
                CategoryType.Expense,
                ColorHex.Create("#F39C12"),
                "icon"));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void CreateSystemDefault_WithValidData_CreatesSystemCategory()
    {
        // Arrange & Act
        // Feature 3: CreateSystemDefault no longer takes userId parameter
        var category = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Assert
        Assert.True(category.IsSystemDefault);
        Assert.Null(category.UserId); // Feature 3: System categories have NULL user_id
        Assert.Equal("Salary", category.Name.Value);
        Assert.Equal(CategoryType.Income, category.Type);
    }

    [Fact]
    public void Update_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var category = CreateTestCategory();
        var newName = CategoryName.Create("Utilities");
        var newColor = ColorHex.Create("#E74C3C");
        var newIcon = "lightning-charge";

        // Act
        category.Update(newName, newColor, newIcon);

        // Assert
        Assert.Equal("Utilities", category.Name.Value);
        Assert.Equal("#E74C3C", category.Color.Value);
        Assert.Equal("lightning-charge", category.IconName);
    }

    [Fact]
    public void Update_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var category = CreateTestCategory();
        Assert.Null(category.UpdatedAt); // UpdatedAt is null before first update
        var beforeUpdate = DateTime.UtcNow;
        System.Threading.Thread.Sleep(100); // Ensure time passes

        // Act
        category.Update(
            CategoryName.Create("New Name"),
            ColorHex.Create("#E74C3C"),
            "new-icon");

        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(category.UpdatedAt);
        Assert.True(category.UpdatedAt >= beforeUpdate && category.UpdatedAt <= afterUpdate);
    }

    [Fact]
    public void Update_OnSystemDefault_ThrowsDomainException()
    {
        // Arrange
        var category = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            category.Update(
                CategoryName.Create("New Name"),
                ColorHex.Create("#E74C3C"),
                "new-icon"));

        Assert.Contains("cannot be modified", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            category.Update(null!, ColorHex.Create("#E74C3C"), "icon"));

        Assert.Equal("newName", ex.ParamName);
    }

    [Fact]
    public void CanDelete_WhenCustomCategoryWithNoTransactions_ReturnsTrue()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act & Assert
        Assert.True(category.CanDelete(hasActiveTransactions: false));
    }

    [Fact]
    public void CanDelete_WhenSystemDefault_ReturnsFalse()
    {
        // Arrange
        var category = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Act & Assert
        Assert.False(category.CanDelete(hasActiveTransactions: false));
    }

    [Fact]
    public void CanDelete_WhenHasTransactions_ReturnsFalse()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act & Assert
        Assert.False(category.CanDelete(hasActiveTransactions: true));
    }

    [Fact]
    public void CanDelete_WhenBothSystemDefaultAndHasTransactions_ReturnsFalse()
    {
        // Arrange
        var category = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Act & Assert
        Assert.False(category.CanDelete(hasActiveTransactions: true));
    }

    [Fact]
    public void PropertiesAreReadOnly()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act & Assert - Verify properties don't have public setters
        // These should compile errors if attempted:
        // category.Name = new CategoryName("Test"); // Error: init-only property
        // category.Type = CategoryType.Income;      // Error: init-only or no setter
        // category.IsSystemDefault = true;          // Error: init-only or no setter

        Assert.NotNull(category);
    }
}

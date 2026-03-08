namespace SauronSheet.Domain.Tests.Entities;

using System;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Feature 3: Unit tests for nullable UserId in Category entity.
/// Tests NULL semantics, helper methods, and domain invariant validation.
/// </summary>
[Trait("Category", "Domain")]
public class CategoryNullableUserIdTests
{
    private readonly UserId _testUserId = new("user-123");

    // T-3.01: Category_SystemDefault_HasNullUserId
    [Fact]
    public void CreateSystemDefault_HasNullUserId()
    {
        // Arrange & Act
        var category = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Assert
        Assert.Null(category.UserId);
        Assert.True(category.IsSystemDefault);
        Assert.True(category.IsGlobal);
        Assert.False(category.IsUserScoped);
    }

    // T-3.02: Category_UserScoped_HasNonNullUserId
    [Fact]
    public void Constructor_UserScoped_HasNonNullUserId()
    {
        // Arrange & Act
        var category = new Category(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create("Coffee"),
            CategoryType.Expense,
            ColorHex.Create("#9B59B6"),
            "coffee");

        // Assert
        Assert.NotNull(category.UserId);
        Assert.Equal("user-123", category.UserId.Value);
        Assert.False(category.IsSystemDefault);
        Assert.False(category.IsGlobal);
        Assert.True(category.IsUserScoped);
    }

    // T-3.03: Category_IsGlobal_ReturnsTrueForNull
    [Fact]
    public void IsGlobal_ReturnsTrueForNullUserId()
    {
        // Arrange
        var systemCategory = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        var userCategory = new Category(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create("Coffee"),
            CategoryType.Expense,
            ColorHex.Create("#9B59B6"),
            "coffee");

        // Act & Assert
        Assert.True(systemCategory.IsGlobal);
        Assert.False(userCategory.IsGlobal);
    }

    // T-3.04: Category_IsUserScoped_ReturnsTrueForNonNull
    [Fact]
    public void IsUserScoped_ReturnsTrueForNonNullUserId()
    {
        // Arrange
        var userCategory = new Category(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create("Coffee"),
            CategoryType.Expense,
            ColorHex.Create("#9B59B6"),
            "coffee");

        var systemCategory = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Act & Assert
        Assert.True(userCategory.IsUserScoped);
        Assert.False(systemCategory.IsUserScoped);
    }

    // T-3.05: Category_NullUserIdWithSystemDefaultFalse_ThrowsDomainException
    [Fact]
    public void PrivateConstructor_NullUserIdWithSystemDefaultFalse_ThrowsDomainException()
    {
        // Arrange
        var categoryType = CategoryType.Expense;
        var name = CategoryName.Create("Invalid");
        var color = ColorHex.Create("#F39C12");
        var icon = "basket";

        // Act & Assert
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
        {
            // Use reflection to call private constructor with invalid state
            var ctor = typeof(Category).GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];

            ctor.Invoke(new object?[] 
            { 
                CategoryId.New(),           // id
                (UserId?)null,              // userId (null)
                name,                       // name
                categoryType,               // type
                color,                      // color
                icon,                       // iconName
                false                       // isSystemDefault (FALSE - invalid!)
            });
        });

        // Assert - InnerException should be DomainException
        Assert.IsType<DomainException>(ex.InnerException);
        Assert.Contains("null UserId must be marked as system defaults", ex.InnerException?.Message ?? "");
    }

    // T-3.06: Category_IsOwnedByUser_ReturnsTrueForOwner
    [Fact]
    public void IsOwnedByUser_ReturnsTrueForOwner()
    {
        // Arrange
        var userId = new UserId("user-123");
        var otherUserId = new UserId("user-456");
        
        var userCategory = new Category(
            CategoryId.New(),
            userId,
            CategoryName.Create("Coffee"),
            CategoryType.Expense,
            ColorHex.Create("#9B59B6"),
            "coffee");

        var systemCategory = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Act & Assert
        Assert.True(userCategory.IsOwnedByUser(userId));
        Assert.False(userCategory.IsOwnedByUser(otherUserId));
        Assert.False(systemCategory.IsOwnedByUser(userId)); // System categories not "owned"
    }

    // T-3.07: Category_IsAccessibleToUser_AllowsSystemDefault
    [Fact]
    public void IsAccessibleToUser_AllowsSystemDefault()
    {
        // Arrange
        var userId = new UserId("user-123");
        var otherUserId = new UserId("user-456");

        var userCategory = new Category(
            CategoryId.New(),
            userId,
            CategoryName.Create("Coffee"),
            CategoryType.Expense,
            ColorHex.Create("#9B59B6"),
            "coffee");

        var systemCategory = Category.CreateSystemDefault(
            CategoryId.New(),
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Act & Assert
        // User can access own category
        Assert.True(userCategory.IsAccessibleToUser(userId));
        
        // User CANNOT access others' categories
        Assert.False(userCategory.IsAccessibleToUser(otherUserId));
        
        // User CAN access system categories
        Assert.True(systemCategory.IsAccessibleToUser(userId));
        Assert.True(systemCategory.IsAccessibleToUser(otherUserId));
    }
}

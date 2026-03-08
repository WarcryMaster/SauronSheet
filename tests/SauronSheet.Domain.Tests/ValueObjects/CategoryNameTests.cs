namespace SauronSheet.Domain.Tests.ValueObjects;

using System;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class CategoryNameTests
{
    [Fact]
    public void Create_WithValidName_ReturnsSuccessfully()
    {
        // Arrange & Act
        var categoryName = CategoryName.Create("Groceries");

        // Assert
        Assert.NotNull(categoryName);
        Assert.Equal("Groceries", categoryName.Value);
    }

    [Fact]
    public void Create_WithMinimumLength_ReturnsSuccessfully()
    {
        // Arrange & Act
        var categoryName = CategoryName.Create("F");

        // Assert
        Assert.Equal("F", categoryName.Value);
    }

    [Fact]
    public void Create_WithMaximumLength_ReturnsSuccessfully()
    {
        // Arrange
        var name = new string('A', 50);

        // Act
        var categoryName = CategoryName.Create(name);

        // Assert
        Assert.Equal(name, categoryName.Value);
    }

    [Fact]
    public void Create_WithLeadingAndTrailingWhitespace_TrimsSuccessfully()
    {
        // Arrange & Act
        var categoryName = CategoryName.Create("  Groceries  ");

        // Assert
        Assert.Equal("Groceries", categoryName.Value);
    }

    [Fact]
    public void Create_WithEmptyString_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => CategoryName.Create(""));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => CategoryName.Create("   "));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNull_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => CategoryName.Create(null!));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ExceedsMaxLength_ThrowsDomainException()
    {
        // Arrange
        var name = new string('A', 51);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => CategoryName.Create(name));
        Assert.Contains("exceed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithValidName_ReturnsTrue()
    {
        // Arrange
        var categoryName = new CategoryName("Groceries");

        // Act & Assert
        Assert.True(categoryName.Validate());
    }

    [Fact]
    public void Validate_WithEmptyValue_ReturnsFalse()
    {
        // Arrange
        var categoryName = new CategoryName("");

        // Act & Assert
        Assert.False(categoryName.Validate());
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var name1 = new CategoryName("Groceries");
        var name2 = new CategoryName("Groceries");

        // Act & Assert
        Assert.Equal(name1, name2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var name1 = new CategoryName("Groceries");
        var name2 = new CategoryName("Utilities");

        // Act & Assert
        Assert.NotEqual(name1, name2);
    }

    [Fact]
    public void CanBeUsedAsHashKey()
    {
        // Arrange
        var name1 = CategoryName.Create("Groceries");
        var name2 = CategoryName.Create("Groceries");
        var dict = new Dictionary<CategoryName, string> { { name1, "value1" } };

        // Act & Assert
        Assert.True(dict.ContainsKey(name2));
    }
}

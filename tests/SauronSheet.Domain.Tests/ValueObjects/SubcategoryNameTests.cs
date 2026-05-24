namespace SauronSheet.Domain.Tests.ValueObjects;

using System;
using Xunit;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class SubcategoryNameTests
{
    [Fact]
    public void Create_WithValidName_ReturnsSuccessfully()
    {
        // Arrange & Act
        var name = SubcategoryName.Create("Ropa y complementos");

        // Assert
        Assert.NotNull(name);
        Assert.Equal("Ropa y complementos", name.Value);
    }

    [Fact]
    public void Create_WithMinimumLength_ReturnsSuccessfully()
    {
        // Arrange & Act
        var name = SubcategoryName.Create("A");

        // Assert
        Assert.Equal("A", name.Value);
    }

    [Fact]
    public void Create_WithMaximumLength_ReturnsSuccessfully()
    {
        // Arrange
        var value = new string('A', 50);

        // Act
        var name = SubcategoryName.Create(value);

        // Assert
        Assert.Equal(value, name.Value);
    }

    [Fact]
    public void Create_WithLeadingAndTrailingWhitespace_TrimsSuccessfully()
    {
        // Arrange & Act
        var name = SubcategoryName.Create("  Ropa  ");

        // Assert
        Assert.Equal("Ropa", name.Value);
    }

    [Fact]
    public void Create_WithEmptyString_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => SubcategoryName.Create(""));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => SubcategoryName.Create("   "));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNull_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => SubcategoryName.Create(null!));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ExceedsMaxLength_ThrowsDomainException()
    {
        // Arrange
        var value = new string('A', 51);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => SubcategoryName.Create(value));
        Assert.Contains("exceed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var name1 = SubcategoryName.Create("Ropa");
        var name2 = SubcategoryName.Create("Ropa");

        // Act & Assert
        Assert.Equal(name1, name2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var name1 = SubcategoryName.Create("Ropa");
        var name2 = SubcategoryName.Create("Complementos");

        // Act & Assert
        Assert.NotEqual(name1, name2);
    }

    [Fact]
    public void CanBeUsedAsHashKey()
    {
        // Arrange
        var name1 = SubcategoryName.Create("Ropa");
        var name2 = SubcategoryName.Create("Ropa");
        var dict = new Dictionary<SubcategoryName, string> { { name1, "value1" } };

        // Act & Assert
        Assert.True(dict.ContainsKey(name2));
    }

    [Fact]
    public void Validate_WithValidName_ReturnsTrue()
    {
        // Arrange
        var name = new SubcategoryName("Ropa");

        // Act & Assert
        Assert.True(name.Validate());
    }

    [Fact]
    public void Validate_WithEmptyValue_ReturnsFalse()
    {
        // Arrange
        var name = new SubcategoryName("");

        // Act & Assert
        Assert.False(name.Validate());
    }
}

namespace SauronSheet.Domain.Tests.ValueObjects;

using System;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class ColorHexTests
{
    [Fact]
    public void Create_WithValidHexUppercase_ReturnsSuccessfully()
    {
        // Arrange & Act
        var color = ColorHex.Create("#F39C12");

        // Assert
        Assert.NotNull(color);
        Assert.Equal("#F39C12", color.Value);
    }

    [Fact]
    public void Create_WithValidHexLowercase_NormalizesToUppercase()
    {
        // Arrange & Act
        var color = ColorHex.Create("#f39c12");

        // Assert
        Assert.Equal("#F39C12", color.Value);
    }

    [Fact]
    public void Create_WithMixedCase_NormalizesToUppercase()
    {
        // Arrange & Act
        var color = ColorHex.Create("#F3Ac12");

        // Assert
        Assert.Equal("#F3AC12", color.Value);
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#FF0000")]
    [InlineData("#27AE60")]
    [InlineData("#E74C3C")]
    public void Create_WithValidColors_ReturnsSuccessfully(string hex)
    {
        // Arrange & Act
        var color = ColorHex.Create(hex);

        // Assert
        Assert.Equal(hex, color.Value);
    }

    [Fact]
    public void Create_WithEmptyString_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => ColorHex.Create(""));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNull_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => ColorHex.Create(null!));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithWhitespaceOnly_ThrowsDomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => ColorHex.Create("   "));
        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("F39C12")]  // Missing #
    [InlineData("#F39C1")]  // Too short
    [InlineData("#F39C123")] // Too long
    [InlineData("#GGGGGG")] // Invalid characters
    [InlineData("#F39G12")] // Invalid character in middle
    public void Create_WithInvalidFormat_ThrowsDomainException(string hex)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => ColorHex.Create(hex));
        Assert.Contains("valid hex code", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithValidHex_ReturnsTrue()
    {
        // Arrange
        var color = new ColorHex("#F39C12");

        // Act & Assert
        Assert.True(color.Validate());
    }

    [Fact]
    public void Validate_WithInvalidHex_ReturnsFalse()
    {
        // Arrange
        var color = new ColorHex("F39C12");

        // Act & Assert
        Assert.False(color.Validate());
    }

    [Fact]
    public void Validate_WithEmptyValue_ReturnsFalse()
    {
        // Arrange
        var color = new ColorHex("");

        // Act & Assert
        Assert.False(color.Validate());
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var color1 = new ColorHex("#F39C12");
        var color2 = new ColorHex("#F39C12");

        // Act & Assert
        Assert.Equal(color1, color2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var color1 = new ColorHex("#F39C12");
        var color2 = new ColorHex("#27AE60");

        // Act & Assert
        Assert.NotEqual(color1, color2);
    }

    [Fact]
    public void CanBeUsedAsHashKey()
    {
        // Arrange
        var color1 = ColorHex.Create("#F39C12");
        var color2 = ColorHex.Create("#F39C12");
        var dict = new Dictionary<ColorHex, string> { { color1, "value1" } };

        // Act & Assert
        Assert.True(dict.ContainsKey(color2));
    }
}

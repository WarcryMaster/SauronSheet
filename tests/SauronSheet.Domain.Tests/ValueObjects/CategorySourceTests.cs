namespace SauronSheet.Domain.Tests.ValueObjects;

using Xunit;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class CategorySourceTests
{
    [Fact]
    public void Legacy_IsZero()
    {
        // Arrange & Act
        var source = CategorySource.Legacy;

        // Assert
        Assert.Equal(0, (int)source);
    }

    [Fact]
    public void RawOnly_IsOne()
    {
        // Arrange & Act
        var source = CategorySource.RawOnly;

        // Assert
        Assert.Equal(1, (int)source);
    }

    [Fact]
    public void AutoMatched_IsTwo()
    {
        // Arrange & Act
        var source = CategorySource.AutoMatched;

        // Assert
        Assert.Equal(2, (int)source);
    }

    [Fact]
    public void UserOverride_IsThree()
    {
        // Arrange & Act
        var source = CategorySource.UserOverride;

        // Assert
        Assert.Equal(3, (int)source);
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        // Arrange & Act & Assert
        Assert.Equal("Legacy", CategorySource.Legacy.ToString());
        Assert.Equal("RawOnly", CategorySource.RawOnly.ToString());
        Assert.Equal("AutoMatched", CategorySource.AutoMatched.ToString());
        Assert.Equal("UserOverride", CategorySource.UserOverride.ToString());
    }

    [Fact]
    public void ParseFromString_ReturnsCorrectValue()
    {
        // Arrange & Act & Assert
        Assert.Equal(CategorySource.Legacy, Enum.Parse<CategorySource>("Legacy"));
        Assert.Equal(CategorySource.RawOnly, Enum.Parse<CategorySource>("RawOnly"));
        Assert.Equal(CategorySource.AutoMatched, Enum.Parse<CategorySource>("AutoMatched"));
        Assert.Equal(CategorySource.UserOverride, Enum.Parse<CategorySource>("UserOverride"));
    }

    [Fact]
    public void AllValues_AreFourDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<CategorySource>();

        // Assert
        Assert.Equal(4, values.Length);
        Assert.Contains(CategorySource.Legacy, values);
        Assert.Contains(CategorySource.RawOnly, values);
        Assert.Contains(CategorySource.AutoMatched, values);
        Assert.Contains(CategorySource.UserOverride, values);
    }

    [Fact]
    public void DefaultValue_IsLegacy()
    {
        // Arrange & Act
        var source = default(CategorySource);

        // Assert
        Assert.Equal(CategorySource.Legacy, source);
        Assert.Equal(0, (int)source);
    }
}

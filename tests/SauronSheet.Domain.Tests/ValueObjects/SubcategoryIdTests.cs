namespace SauronSheet.Domain.Tests.ValueObjects;

using System;
using Xunit;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class SubcategoryIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_CreatesSuccessfully()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = new SubcategoryId(guid);

        // Assert
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void New_ReturnsNonEmptyId()
    {
        // Act
        var id = SubcategoryId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_ReturnsDifferentIdsEachCall()
    {
        // Act
        var id1 = SubcategoryId.New();
        var id2 = SubcategoryId.New();

        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void EmptyConstructor_ThrowsDomainException()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<DomainException>(() => new SubcategoryId());
        Assert.Contains("cannot be empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new SubcategoryId(guid);
        var id2 = new SubcategoryId(guid);

        // Act & Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var id1 = new SubcategoryId(Guid.NewGuid());
        var id2 = new SubcategoryId(Guid.NewGuid());

        // Act & Assert
        Assert.NotEqual(id1, id2);
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
    }

    [Fact]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var id = SubcategoryId.New();
        var dict = new Dictionary<SubcategoryId, string> { { id, "value1" } };

        // Act & Assert
        Assert.True(dict.ContainsKey(id));
        Assert.Equal("value1", dict[id]);
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new SubcategoryId(guid);
        var id2 = new SubcategoryId(guid);

        // Act & Assert
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }
}

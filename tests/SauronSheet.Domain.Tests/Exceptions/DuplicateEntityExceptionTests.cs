using SauronSheet.Domain.Exceptions;
using Xunit;

namespace SauronSheet.Domain.Tests.Exceptions;

/// <summary>
/// Unit tests for DuplicateEntityException.
/// Verifies all three constructors, message format, and inner exception chaining.
/// </summary>
public class DuplicateEntityExceptionTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void DuplicateEntityException_EntityNameAndKey_FormatsMessage()
    {
        // Arrange
        const string entityName = "Category";
        const string conflictingKey = "food";

        // Act
        var ex = new DuplicateEntityException(entityName, conflictingKey);

        // Assert
        Assert.Contains("Duplicate", ex.Message);
        Assert.Contains(entityName, ex.Message);
        Assert.Contains(conflictingKey, ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DuplicateEntityException_MessageOnly_StoresMessage()
    {
        // Arrange
        const string message = "Custom duplicate error.";

        // Act
        var ex = new DuplicateEntityException(message);

        // Assert
        Assert.Equal(message, ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DuplicateEntityException_MessageAndInner_ChainsInnerException()
    {
        // Arrange
        const string message = "Wrap error.";
        var inner = new InvalidOperationException("root cause");

        // Act
        var ex = new DuplicateEntityException(message, inner);

        // Assert
        Assert.Equal(message, ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DuplicateEntityException_IsDomainException()
    {
        // Arrange & Act
        var ex = new DuplicateEntityException("Entity", "key");

        // Assert
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

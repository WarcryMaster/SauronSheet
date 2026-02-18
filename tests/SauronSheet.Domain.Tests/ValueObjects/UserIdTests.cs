using Xunit;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.ValueObjects;

public class UserIdTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_ValidString_SetsValue()
    {
        // Arrange
        string validUserId = "user-123";

        // Act
        var userId = new UserId(validUserId);

        // Assert
        Assert.Equal(validUserId, userId.Value);
        Assert.Equal(validUserId, userId.ToString());
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_NullString_ThrowsDomainException()
    {
        // Arrange
        string? nullUserId = null;

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new UserId(nullUserId!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_EmptyString_ThrowsDomainException()
    {
        // Arrange
        string emptyUserId = "";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new UserId(emptyUserId));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_WhitespaceString_ThrowsDomainException()
    {
        // Arrange
        string whitespaceUserId = "   ";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new UserId(whitespaceUserId));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_Equality_SameValue()
    {
        // Arrange
        var userId1 = new UserId("user-123");
        var userId2 = new UserId("user-123");

        // Act & Assert
        Assert.Equal(userId1, userId2);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_Inequality_DifferentValue()
    {
        // Arrange
        var userId1 = new UserId("user-123");
        var userId2 = new UserId("user-456");

        // Act & Assert
        Assert.NotEqual(userId1, userId2);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_SuccessFactory_SetsProperties()
    {
        // Arrange
        var userId = new UserId("user-123");
        string accessToken = "access-token";
        string refreshToken = "refresh-token";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var result = AuthResult.Success(userId, accessToken, refreshToken, expiresAt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(refreshToken, result.RefreshToken);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_FailureFactory_SetsError()
    {
        // Arrange
        string errorMessage = "Invalid credentials";

        // Act
        var result = AuthResult.Failure(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.UserId);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        Assert.Null(result.ExpiresAt);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }
}

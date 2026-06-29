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
        UserId userId = new UserId(validUserId);

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
        DomainException exception = Assert.Throws<DomainException>(() => new UserId(nullUserId!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_EmptyString_ThrowsDomainException()
    {
        // Arrange
        string emptyUserId = "";

        // Act & Assert
        DomainException exception = Assert.Throws<DomainException>(() => new UserId(emptyUserId));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_WhitespaceString_ThrowsDomainException()
    {
        // Arrange
        string whitespaceUserId = "   ";

        // Act & Assert
        DomainException exception = Assert.Throws<DomainException>(() => new UserId(whitespaceUserId));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_Equality_SameValue()
    {
        // Arrange
        UserId userId1 = new UserId("user-123");
        UserId userId2 = new UserId("user-123");

        // Act & Assert
        Assert.Equal(userId1, userId2);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UserId_Inequality_DifferentValue()
    {
        // Arrange
        UserId userId1 = new UserId("user-123");
        UserId userId2 = new UserId("user-456");

        // Act & Assert
        Assert.NotEqual(userId1, userId2);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_SuccessFactory_SetsProperties()
    {
        // Arrange
        UserId userId = new UserId("user-123");
        string accessToken = "access-token";
        string refreshToken = "refresh-token";
        DateTime expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        AuthResult result = AuthResult.Success(userId, accessToken, refreshToken, expiresAt);

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
        AuthResult result = AuthResult.Failure(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.UserId);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        Assert.Null(result.ExpiresAt);
        Assert.False(result.RequiresEmailConfirmation);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_SuccessWithConfirmationRequiredFactory_SetsExpectedFlags()
    {
        // Arrange
        UserId userId = new UserId("user-confirmation");

        // Act
        AuthResult result = AuthResult.SuccessWithConfirmationRequired(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.UserId);
        Assert.True(result.RequiresEmailConfirmation);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        Assert.Null(result.ExpiresAt);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_SuccessWithoutSessionFactory_DefaultsToNoEmailConfirmation()
    {
        // Act
        AuthResult result = AuthResult.SuccessWithoutSession();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.UserId);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        Assert.Null(result.ExpiresAt);
        Assert.False(result.RequiresEmailConfirmation);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AuthResult_SuccessWithoutSessionFactory_AllowsEmailConfirmationFlag()
    {
        // Act
        AuthResult result = AuthResult.SuccessWithoutSession(requiresEmailConfirmation: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.UserId);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        Assert.Null(result.ExpiresAt);
        Assert.True(result.RequiresEmailConfirmation);
        Assert.Null(result.ErrorMessage);
    }
}

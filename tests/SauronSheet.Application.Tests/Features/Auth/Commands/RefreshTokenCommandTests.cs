using Xunit;
using Moq;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class RefreshTokenCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task RefreshToken_ValidRefresh_ReturnsNewTokens()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var userId = new UserId("user-123");
        var newExpiresAt = DateTime.UtcNow.AddHours(1);
        mockAuthService
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(AuthResult.Success(userId, "new-access-token", "new-refresh-token", newExpiresAt));

        var handler = new RefreshTokenCommandHandler(mockAuthService.Object);
        var command = new RefreshTokenCommand("refresh-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.Equal(newExpiresAt, result.ExpiresAt);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RefreshToken_InvalidRefresh_ThrowsUnauthorized()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(AuthResult.Failure("Session expired"));

        var handler = new RefreshTokenCommandHandler(mockAuthService.Object);
        var command = new RefreshTokenCommand("invalid-token");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Session expired", exception.Message);
    }
}

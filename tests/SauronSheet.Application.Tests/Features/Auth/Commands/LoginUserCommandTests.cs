using Xunit;
using Moq;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class LoginUserCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task LoginUser_ValidCredentials_ReturnsAuthToken()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var userId = new UserId("user-123");
        var expiresAt = DateTime.UtcNow.AddHours(1);
        mockAuthService
            .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(AuthResult.Success(userId, "access-token", "refresh-token", expiresAt));

        var handler = new LoginUserCommandHandler(mockAuthService.Object);
        var command = new LoginUserCommand("test@example.com", "password123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.Equal("user-123", result.UserId);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task LoginUser_InvalidCredentials_ThrowsUnauthorized()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(AuthResult.Failure("Invalid credentials"));

        var handler = new LoginUserCommandHandler(mockAuthService.Object);
        var command = new LoginUserCommand("test@example.com", "wrongpassword");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Invalid email or password", exception.Message);
    }
}

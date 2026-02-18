using Xunit;
using Moq;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class RegisterUserCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_ValidInput_ReturnsRegistrationResult()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var userId = new UserId("user-123");
        mockAuthService
            .Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(AuthResult.Success(userId, "access-token", "refresh-token", DateTime.UtcNow.AddHours(1)));

        var handler = new RegisterUserCommandHandler(mockAuthService.Object);
        var command = new RegisterUserCommand("test@example.com", "password123", "password123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("test@example.com", result.Email);
        mockAuthService.Verify(x => x.RegisterAsync("test@example.com", "password123"), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_DuplicateEmail_ThrowsDomainException()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(AuthResult.Failure("Email is already registered"));

        var handler = new RegisterUserCommandHandler(mockAuthService.Object);
        var command = new RegisterUserCommand("existing@example.com", "password123", "password123");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_WeakPassword_ThrowsDomainException()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var handler = new RegisterUserCommandHandler(mockAuthService.Object);
        var command = new RegisterUserCommand("test@example.com", "short", "short");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("at least 8 characters", exception.Message);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task RegisterUser_MismatchedPasswords_ThrowsDomainException()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var handler = new RegisterUserCommandHandler(mockAuthService.Object);
        var command = new RegisterUserCommand("test@example.com", "password123", "password456");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("do not match", exception.Message);
        mockAuthService.Verify(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

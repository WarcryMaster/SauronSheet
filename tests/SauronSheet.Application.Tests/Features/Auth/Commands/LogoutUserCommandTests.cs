using Xunit;
using Moq;
using SauronSheet.Application.Features.Auth.Commands;
using SauronSheet.Domain.Services;

namespace SauronSheet.Application.Tests.Features.Auth.Commands;

public class LogoutUserCommandTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task LogoutUser_ValidToken_CallsAuthService()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(x => x.LogoutAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var handler = new LogoutUserCommandHandler(mockAuthService.Object);
        var command = new LogoutUserCommand("valid-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        mockAuthService.Verify(x => x.LogoutAsync("valid-token"), Times.Once);
    }
}

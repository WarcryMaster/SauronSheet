using SauronSheet.Domain.Common;
using Xunit;
using Moq;
using SauronSheet.Application.Features.Auth.Queries;

using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Auth.Queries;

public class GetCurrentUserQueryTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCurrentUser_Authenticated_ReturnsProfile()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockUserContext = new Mock<IUserContext>();

        var userId = new UserId("user-123");
        var userProfile = new UserProfile(userId, "test@example.com", "Test User", DateTime.UtcNow);

        mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        mockUserContext.Setup(x => x.UserId).Returns("user-123");

        mockAuthService
            .Setup(x => x.GetUserProfileAsync(It.IsAny<string>()))
            .ReturnsAsync(userProfile);

        var handler = new GetCurrentUserQueryHandler(mockAuthService.Object, mockUserContext.Object);
        var query = new GetCurrentUserQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-123", result.UserId);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetCurrentUser_Unauthenticated_ThrowsUnauthorized()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthService>();
        var mockUserContext = new Mock<IUserContext>();

        mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);

        var handler = new GetCurrentUserQueryHandler(mockAuthService.Object, mockUserContext.Object);
        var query = new GetCurrentUserQuery();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Contains("not authenticated", exception.Message);
    }
}

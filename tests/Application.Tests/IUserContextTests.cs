namespace Application.Tests;

using Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests for IUserContext and dependency injection
/// </summary>
public class IUserContextTests
{
    /// <summary>
    /// T00-007: MockUserContext injects correctly from DI container + returns mocked UserId
    /// </summary>
    [Fact]
    public void MockUserContext_Should_Inject_From_DI_Container()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var userContext = serviceProvider.GetRequiredService<IUserContext>();
        var userId = userContext.UserId;

        // Assert
        Assert.NotNull(userContext);
        Assert.IsType<MockUserContext>(userContext);
        Assert.NotEqual(Guid.Empty, userId);
    }

    [Fact]
    public void MockUserContext_Should_Have_Consistent_UserId()
    {
        // Arrange
        var mockContext = new MockUserContext { UserId = Guid.NewGuid() };
        var userId1 = mockContext.UserId;
        var userId2 = mockContext.UserId;

        // Act & Assert
        Assert.Equal(userId1, userId2);
    }
}

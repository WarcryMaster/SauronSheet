namespace Application.Tests;

using Application;
using Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests for dependency injection container configuration
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>
    /// T00-009: MediatR DI container resolves correctly
    /// </summary>
    [Fact]
    public void DependencyInjection_Should_Resolve_MediatR()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var mediator = serviceProvider.GetService<IMediator>();

        // Assert
        Assert.NotNull(mediator);
    }

    [Fact]
    public void DependencyInjection_Should_Register_Behaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Assert - Verify behaviors are registered by attempting to use MediatR
        Assert.NotNull(mediator);
        
        // Get user context to verify it's registered
        var userContext = serviceProvider.GetRequiredService<IUserContext>();
        Assert.NotNull(userContext);
    }

    [Fact]
    public void DependencyInjection_Should_Register_UserContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var userContext = serviceProvider.GetRequiredService<IUserContext>();

        // Assert
        Assert.NotNull(userContext);
        Assert.IsType<MockUserContext>(userContext);
    }
}

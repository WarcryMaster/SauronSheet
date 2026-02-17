using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SauronSheet.Application;
using Xunit;

namespace SauronSheet.Application.Tests.Common;

/// <summary>
/// Integration tests for MediatR registration in Application layer.
/// Verifies that dependency injection is correctly configured.
/// </summary>
public class MediatRRegistrationTests
{
    [Fact]
    [Trait("Category", "Application")]
    public void MediatR_ResolvesFromServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();

        Assert.NotNull(mediator);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void AddApplicationServices_RegistersWithoutException()
    {
        var services = new ServiceCollection();

        // Should not throw
        services.AddApplicationServices();

        Assert.NotEmpty(services);
    }
}

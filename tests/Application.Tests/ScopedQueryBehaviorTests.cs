namespace Application.Tests;

using Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests for ScopedQueryBehavior multi-tenancy enforcement
/// </summary>
public class ScopedQueryBehaviorTests
{
    private class TestRequest : IRequest<TestResponse>
    {
    }

    private class TestResponse : ITenantScoped
    {
        public required Guid TenantId { get; set; }
    }

    private class TestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestResponse { TenantId = Guid.NewGuid() });
        }
    }

    /// <summary>
    /// T00-008: ScopedQueryBehavior blocks cross-tenant queries
    /// </summary>
    [Fact]
    public async Task ScopedQueryBehavior_Should_Block_Cross_Tenant_Access()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();

        // Override user context with specific user
        services.AddScoped<IUserContext>(sp => new MockUserContext { UserId = userIdA });
        services.AddScoped(typeof(IRequestHandler<TestRequest, TestResponse>), typeof(TestHandler));

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert - Should fail because handler returns TenantId != UserId
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await mediator.Send(new TestRequest())
        );
    }

    [Fact]
    public async Task ScopedQueryBehavior_Should_Allow_Same_Tenant_Access()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var userId = Guid.NewGuid();
        
        services.AddScoped<IUserContext>(sp => new MockUserContext { UserId = userId });
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ScopedQueryBehavior<,>));
        });

        // Create a handler that returns correct tenant
        services.AddScoped<IRequestHandler<TestTenantRequest, TestTenantResponse>>(
            sp => new TestTenantHandler(userId)
        );

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new TestTenantRequest());

        // Assert
        Assert.Equal(userId, result.TenantId);
    }

    private class TestTenantRequest : IRequest<TestTenantResponse>
    {
    }

    private class TestTenantResponse : ITenantScoped
    {
        public required Guid TenantId { get; set; }
    }

    private class TestTenantHandler : IRequestHandler<TestTenantRequest, TestTenantResponse>
    {
        private readonly Guid _userId;

        public TestTenantHandler(Guid userId)
        {
            _userId = userId;
        }

        public Task<TestTenantResponse> Handle(TestTenantRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestTenantResponse { TenantId = _userId });
        }
    }
}

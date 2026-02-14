namespace Application;

using Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Application layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Application layer services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR with behaviors
        services
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(ScopedQueryBehavior<,>));
            });

        // Register user context (mock for Phase 0)
        services.AddScoped<IUserContext, MockUserContext>();

        return services;
    }
}

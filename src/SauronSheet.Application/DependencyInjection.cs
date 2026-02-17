using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace SauronSheet.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// Registers MediatR from Application assembly.
/// Pipeline behaviors (validation, tenant scoping) registered here in later phases.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly)
        );

        return services;
    }
}

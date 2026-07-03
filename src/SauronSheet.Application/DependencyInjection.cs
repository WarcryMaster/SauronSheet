using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SauronSheet.Application.Common;
using SauronSheet.Application.Common.Behaviors;
using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Application.Services;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Services;

namespace SauronSheet.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// Registers MediatR from Application assembly.
/// Registers pipeline behaviors for tenant scoping and cross-cutting concerns.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Register pipeline behaviors for tenant scoping and cross-cutting concerns
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantScopingBehavior<,>));
        });

        // Register HttpUserContext implementation (Phase 1)
        services.AddScoped<IUserContext, HttpUserContext>();

        // Bank category resolution service (Phase 2)
        services.AddScoped<IBankCategoryResolutionService, BankCategoryResolutionService>();

        // Domain services — stateless, registered as singleton
        services.AddSingleton<BudgetCalculationService>();

        // Annual analysis classification engine (PR 2)
        services.AddSingleton<IAnnualClassificationEngine, AnnualClassificationEngine>();

        // Annual analysis localized services (PR 5)
        services.AddScoped<InsightsService>();
        services.AddScoped<AnomalyDetectionService>();

        return services;
    }
}

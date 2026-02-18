using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SauronSheet.Infrastructure.Auth;
using SauronSheet.Domain.Services;

namespace SauronSheet.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// Validates Supabase configuration on startup (fast-fail).
/// Registers auth services and HTTP client.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate Supabase configuration on DI registration (fast-fail)
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Url' is not set.");

        var supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Key' is not set.");

        var jwtSecret = configuration["Supabase:JwtSecret"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:JwtSecret' is not set.");

        // Auth configuration (Phase 1)
        services.Configure<AuthConfiguration>(options =>
        {
            options.JwtSecret = jwtSecret;
        });

        // Auth services (Phase 1)
        services.AddHttpClient<IAuthService, SupabaseAuthService>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(supabaseUrl));

        // Note: HttpUserContext is registered in Application layer DependencyInjection
        // Infrastructure keeps clean dependency flow: Infrastructure -> Domain only

        // TODO: Register Supabase client as singleton in Phase 3+
        // var client = new SupabaseClient(new Uri(supabaseUrl), supabaseKey);
        // services.AddSingleton(client);

        return services;
    }
}

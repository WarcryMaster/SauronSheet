using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SauronSheet.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// Validates Supabase configuration on startup (fast-fail).
/// Registers Supabase client as singleton.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate Supabase configuration on DI registration (not deferred)
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Url' is not set.");

        var supabaseKey = configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Configuration key 'Supabase:Key' is not set.");

        // TODO: Register Supabase client as singleton in Phase 1+
        // var client = new SupabaseClient(new Uri(supabaseUrl), supabaseKey);
        // services.AddSingleton(client);

        return services;
    }
}

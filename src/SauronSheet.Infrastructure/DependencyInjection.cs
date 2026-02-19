using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using SauronSheet.Infrastructure.Auth;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Repositories;
using SauronSheet.Application.Common;
using SauronSheet.Application.Interfaces;
using SauronSheet.Infrastructure.Persistence;
using SauronSheet.Infrastructure.PDF;
using SauronSheet.Infrastructure.PDF.Parsers;

namespace SauronSheet.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// Validates Supabase configuration on startup (fast-fail).
/// Registers auth services, repositories, and PDF parsing (Phase 3).
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
            .ConfigureHttpClient(client =>
            {
                // Ensure trailing slash for relative URI resolution
                var baseUrl = supabaseUrl.TrimEnd('/') + "/";
                client.BaseAddress = new Uri(baseUrl);
            });

        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddHttpContextAccessor();

        // CRITICAL FIX C-1: Supabase client registration (Phase 3)
        services.AddSingleton<Supabase.Client>(sp =>
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };
            return new Supabase.Client(supabaseUrl, supabaseKey, options);
        });

        // Repository implementations (NEW in Phase 3)
        services.AddScoped<ITransactionRepository, SupabaseTransactionRepository>();
        services.AddScoped<ICategoryRepository, SupabaseCategoryRepository>();
        services.AddScoped<IPdfImportRepository, SupabasePdfImportRepository>();

        // PDF parsing (NEW in Phase 3)
        services.AddScoped<IPdfParser, GenericBankPdfParser>();
        services.AddSingleton<PdfParserFactory>();

        // Domain services (NEW in Phase 3)
        services.AddScoped<CategoryService>();

        return services;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
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
        // Scoped: each request gets a client with the user's JWT for RLS compliance
        services.AddScoped<Supabase.Client>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var token = httpContextAccessor.HttpContext?.Request.Cookies["sb-access-token"];

            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(token))
            {
                headers["Authorization"] = $"Bearer {token}";
            }

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false,
                Headers = headers
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

        // Budget persistence and domain service (NEW in Phase 5)
        services.AddScoped<IBudgetRepository, SupabaseBudgetRepository>();
        services.AddScoped<BudgetService>();


        return services;
    }

    /// <summary>
    /// Registers Sentry monitoring (Phase 6 - Polish & Infrastructure).
    /// Configures error tracking with DSN from configuration.
    /// Should be called on WebHostBuilder before building the application.
    /// </summary>
    public static IWebHostBuilder AddSentryMonitoring(
        this IWebHostBuilder webBuilder,
        IConfiguration configuration)
    {
        return webBuilder.UseSentry(o =>
        {
            o.Dsn = configuration["Sentry:Dsn"];
            o.Debug = true;
            o.TracesSampleRate = 1.0;
        });
    }
}

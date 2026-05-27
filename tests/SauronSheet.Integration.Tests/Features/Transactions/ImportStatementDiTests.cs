namespace SauronSheet.Integration.Tests.Features.Transactions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Infrastructure;
using SauronSheet.Infrastructure.Excel;
using SauronSheet.Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Phase 2 (replace-pdf-import-with-excel) — DI registration smoke tests.
///
/// Verifies that <see cref="Infrastructure.DependencyInjection.AddInfrastructureServices"/>
/// correctly registers the new neutral services:
///   - <see cref="IStatementParser"/> → <see cref="IngExcelStatementParser"/>
///   - <see cref="IImportBatchRepository"/> → <see cref="SupabaseImportBatchRepository"/>
///
/// Uses ServiceDescriptor lookup (not full resolution) to avoid requiring a live HTTP context
/// or real Supabase client at test time.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "ExcelImport")]
public class ImportStatementDiTests
{
    private static ServiceCollection BuildServices()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:Key"] = "test-key",
                ["Supabase:JwtSecret"] = "test-jwt-secret-that-is-at-least-32-chars",
            })
            .Build();

        services.AddInfrastructureServices(config);
        return services;
    }

    /// <summary>
    /// IStatementParser is registered and maps to IngExcelStatementParser.
    /// </summary>
    [Fact]
    public void DI_IStatementParser_RegisteredAsIngExcelStatementParser()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IStatementParser));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(IngExcelStatementParser), descriptor.ImplementationType);
    }

    /// <summary>
    /// IImportBatchRepository is registered and maps to SupabaseImportBatchRepository.
    /// </summary>
    [Fact]
    public void DI_IImportBatchRepository_RegisteredAsSupabaseImportBatchRepository()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IImportBatchRepository));

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(SupabaseImportBatchRepository), descriptor.ImplementationType);
    }

    /// <summary>
    /// Triangulation: both registrations are Scoped (matching the Supabase client lifetime).
    /// </summary>
    [Fact]
    public void DI_BothNewServices_RegisteredAsScoped()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var parserDescriptor = services.First(d => d.ServiceType == typeof(IStatementParser));
        var repoDescriptor = services.First(d => d.ServiceType == typeof(IImportBatchRepository));

        // Assert — must be Scoped to match the Supabase client lifetime (per-request RLS)
        Assert.Equal(ServiceLifetime.Scoped, parserDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, repoDescriptor.Lifetime);
    }
}

namespace SauronSheet.Application.Tests.Resources;

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using SauronSheet.Application.Resources;
using Xunit;

/// <summary>
/// Tests for the shared localization resources (REQ-LOC-030).
/// </summary>
[Trait("Category", "Application")]
public class SharedResourcesTests
{
    private static IStringLocalizer<SharedResources> CreateLocalizer()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IStringLocalizer<SharedResources>>();
    }

    private static IDisposable SetCurrentUiCulture(string cultureName)
    {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
        return new CultureRestorer(originalCulture);
    }

    private sealed class CultureRestorer : IDisposable
    {
        private readonly CultureInfo _originalCulture;

        public CultureRestorer(CultureInfo originalCulture)
        {
            _originalCulture = originalCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentUICulture = _originalCulture;
        }
    }

    [Fact]
    public void Indexer_SpanishCulture_ReturnsSpanishValue()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        string actual = localizer["Layout.SkipToContent"];

        Assert.Equal("Saltar al contenido principal", actual);
    }

    [Fact]
    public void Indexer_EnglishCulture_ReturnsEnglishValue()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("en-US");
        string actual = localizer["Layout.SkipToContent"];

        Assert.Equal("Skip to main content", actual);
    }

    [Fact]
    public void Indexer_MissingSpanishKey_FallsBackToEnglish()
    {
        IStringLocalizer<SharedResources> localizer = CreateLocalizer();

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        string actual = localizer["Test.EnglishOnlyKey"];

        Assert.Equal("English only fallback", actual);
    }
}

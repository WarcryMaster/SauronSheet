namespace SauronSheet.Frontend.Tests.Localization;

using System.Text.RegularExpressions;
using SauronSheet.Frontend.Tests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the localized _Layout scaffold (REQ-LOC-070, REQ-LOC-040).
/// </summary>
[Trait("Category", "Frontend")]
public class LayoutLocalizationTests : IClassFixture<LocalizationWebApplicationFactory>
{
    private readonly LocalizationWebApplicationFactory _factory;

    public LayoutLocalizationTests(LocalizationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_LoginWithSpanishCulture_RendersHtmlLangEs()
    {
        HttpClient client = _factory.CreateClient();

        string html = await client.GetStringAsync("/auth/login?culture=es-ES");

        Assert.Contains("<html lang=\"es\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_LoginWithEnglishCulture_RendersHtmlLangEn()
    {
        HttpClient client = _factory.CreateClient();

        string html = await client.GetStringAsync("/auth/login?culture=en-US");

        Assert.Contains("<html lang=\"en\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_AnyPage_RendersI18nScaffold()
    {
        HttpClient client = _factory.CreateClient();

        string html = await client.GetStringAsync("/auth/login");

        Assert.Matches(new Regex(@"window\.__i18n\s*=\s*\{", RegexOptions.IgnoreCase), html);
    }

    [Fact]
    public async Task Get_LoginWithSpanishCulture_RendersJsI18nDictionaryAndSpanishFlatpickrLocale()
    {
        HttpClient client = _factory.CreateClient();

        string html = await client.GetStringAsync("/auth/login?culture=es-ES");

        Assert.Contains("\"chart\"", html, StringComparison.Ordinal);
        Assert.Contains("\"series\"", html, StringComparison.Ordinal);
        Assert.Contains("Ingresos", html, StringComparison.Ordinal);
        Assert.Contains("flatpickr/dist/l10n/es.js", html, StringComparison.Ordinal);
        Assert.Contains("flatpickr.localize(flatpickr.l10ns.es)", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_LoginWithEnglishCulture_RendersEnglishFlatpickrLocale()
    {
        HttpClient client = _factory.CreateClient();

        string html = await client.GetStringAsync("/auth/login?culture=en-US");

        Assert.Contains("\"income\":\"Income\"", html, StringComparison.Ordinal);
        Assert.Contains("flatpickr/dist/l10n/default.js", html, StringComparison.Ordinal);
        Assert.Contains("flatpickr.localize(flatpickr.l10ns.default)", html, StringComparison.Ordinal);
    }
}

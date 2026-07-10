namespace SauronSheet.Frontend.Tests.Localization;

using System.Net;
using SauronSheet.Frontend.Tests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the request localization middleware pipeline
/// (REQ-LOC-001, REQ-LOC-010).
/// </summary>
[Trait("Category", "Frontend")]
[Trait("Category", "Integration")]
public class RequestLocalizationTests : IClassFixture<LocalizationWebApplicationFactory>
{
    private readonly LocalizationWebApplicationFactory _factory;

    public RequestLocalizationTests(LocalizationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static HttpClient CreateClient(LocalizationWebApplicationFactory factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        return client;
    }

    [Fact]
    public async Task CookieCultureWinsOverAcceptLanguage()
    {
        HttpClient client = CreateClient(_factory);
        client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c%3Des-ES%7Cuic%3Des-ES");

        string html = await client.GetStringAsync("/auth/login");

        Assert.Contains("<html lang=\"es\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryStringOverridesCookie()
    {
        HttpClient client = CreateClient(_factory);
        client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c%3Den-US%7Cuic%3Den-US");

        string html = await client.GetStringAsync("/auth/login?culture=es-ES");

        Assert.Contains("<html lang=\"es\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AcceptLanguageSpanishResolvesToSpanish()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9");

        string html = await client.GetStringAsync("/auth/login");

        Assert.Contains("<html lang=\"es\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnsupportedAcceptLanguageFallsBackToEnglish()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN");

        string html = await client.GetStringAsync("/auth/login");

        Assert.Contains("<html lang=\"en\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingCookieAndUnsupportedLanguageFallsBackToEnglish()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "fr-FR");

        string html = await client.GetStringAsync("/auth/login");

        Assert.Contains("<html lang=\"en\"", html, StringComparison.Ordinal);
    }
}

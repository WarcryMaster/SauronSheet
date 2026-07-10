namespace SauronSheet.Frontend.Tests.Localization;

using System.Net;
using SauronSheet.Frontend.Tests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the localized Login page (REQ-LOC-030, REQ-LOC-080).
/// </summary>
[Trait("Category", "Frontend")]
[Trait("Category", "Integration")]
public class LoginLocalizationTests : IClassFixture<LocalizationWebApplicationFactory>
{
    private readonly LocalizationWebApplicationFactory _factory;

    public LoginLocalizationTests(LocalizationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_SpanishCulture_RendersLocalizedStringsAndTestIds()
    {
        HttpClient client = _factory.CreateClient();

        string rawHtml = await client.GetStringAsync("/auth/login?culture=es-ES");
        string html = WebUtility.HtmlDecode(rawHtml);

        Assert.Contains("data-testid=\"login-email\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"login-password\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"login-submit\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"login-register-link\"", html, StringComparison.Ordinal);
        Assert.Contains("Bienvenido de nuevo", html, StringComparison.Ordinal);
        Assert.Contains("Correo electrónico", html, StringComparison.Ordinal);
        Assert.Contains("Iniciar sesión", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_EnglishCulture_RendersLocalizedStringsAndTestIds()
    {
        HttpClient client = _factory.CreateClient();

        string rawHtml = await client.GetStringAsync("/auth/login?culture=en-US");
        string html = WebUtility.HtmlDecode(rawHtml);

        Assert.Contains("data-testid=\"login-email\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"login-register-link\"", html, StringComparison.Ordinal);
        Assert.Contains("Welcome back", html, StringComparison.Ordinal);
        Assert.Contains("Email address", html, StringComparison.Ordinal);
        Assert.Contains("Sign in", html, StringComparison.Ordinal);
    }
}

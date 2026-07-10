namespace SauronSheet.Frontend.Tests.Localization;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using SauronSheet.Frontend.Tests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the POST /api/culture language switcher endpoint
/// (REQ-LOC-020).
/// </summary>
[Trait("Category", "Frontend")]
[Trait("Category", "Integration")]
public class CultureEndpointTests : IClassFixture<LocalizationWebApplicationFactory>
{
    private readonly LocalizationWebApplicationFactory _factory;

    public CultureEndpointTests(LocalizationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_ValidSpanishCulture_SetsCookieAndRedirectsToReferer()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Referrer = new Uri("http://localhost/auth/login");

        HttpResponseMessage response = await client.PostAsync("/api/culture?c=es", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/login", response.Headers.Location?.PathAndQuery);

        string? setCookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(setCookie);
        Assert.Contains(".AspNetCore.Culture", setCookie, StringComparison.Ordinal);
        Assert.Contains("es-ES", setCookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_InvalidCulture_FallsBackToEnglishCookie()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Referrer = new Uri("http://localhost/");

        HttpResponseMessage response = await client.PostAsync("/api/culture?c=fr", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        string? setCookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(setCookie);
        Assert.Contains("en-US", setCookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_MissingCulture_ReturnsBadRequest()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.PostAsync("/api/culture", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

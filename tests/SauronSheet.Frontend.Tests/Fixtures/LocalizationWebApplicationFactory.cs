using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SauronSheet.Infrastructure.Auth;

namespace SauronSheet.Frontend.Tests.Fixtures;

public sealed class LocalizationWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:Key"] = "test-key",
                ["Supabase:JwtSecret"] = "test-jwt-secret-that-is-at-least-32-chars",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });

            services.AddSingleton<IHttpClientFactory>(_ =>
            {
                Dictionary<string, HttpResponseMessage> responses = new()
                {
                    {
                        "/.well-known/jwks.json",
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonSerializer.Serialize(new { keys = Array.Empty<object>() }))
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
                            }
                        }
                    }
                };

                return new FakeHttpClientFactory(responses);
            });
        });
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(Dictionary<string, HttpResponseMessage> responses)
        {
            _client = new HttpClient(new FakeHttpMessageHandler(responses));
        }

        public HttpClient CreateClient(string name) => _client;
    }
}


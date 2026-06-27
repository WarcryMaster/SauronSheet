using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Infrastructure.Auth;

namespace SauronSheet.Frontend.Tests.Fixtures;

/// <summary>
/// Web application factory for integration tests of the Annual analysis page.
/// Configures test authentication, mocks IMediator, and stubs external HTTP calls.
/// </summary>
public sealed class AnnualWebApplicationFactory : WebApplicationFactory<Program>
{
    private AnnualAnalysisResultDto? _analysisResult;

    public AnnualWebApplicationFactory WithAnalysisResult(AnnualAnalysisResultDto result)
    {
        _analysisResult = result;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace default authentication with the test scheme.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });

            // Mock the mediator so the annual analysis handler is bypassed.
            services.AddSingleton(_ =>
            {
                Mock<IMediator> mediator = new();

                mediator
                    .Setup(m => m.Send(
                        It.IsAny<GetAnnualAnalysisQuery>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_analysisResult ?? CreateEmptyResult());

                return mediator.Object;
            });

            // Replace IHttpClientFactory so JWT middleware does not call real Supabase JWKS.
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

    private static AnnualAnalysisResultDto CreateEmptyResult()
    {
        return new AnnualAnalysisResultDto(
            Year: DateTime.UtcNow.Year,
            Rows: Array.Empty<AnnualAnalysisRowDto>(),
            Summary: new AnnualAnalysisSummaryDto(
                IncomeFixed: 0m,
                IncomeVariable: 0m,
                IncomeTotal: 0m,
                ExpenseFixed: 0m,
                ExpenseVariable: 0m,
                ExpenseTotal: 0m,
                Net: 0m,
                Currency: "EUR"),
            HasData: false,
            Currency: "EUR");
    }

    /// <summary>
    /// Minimal IHttpClientFactory implementation backed by a fake message handler.
    /// </summary>
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

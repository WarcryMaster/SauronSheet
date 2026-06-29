using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Infrastructure.Auth;

namespace SauronSheet.Frontend.Tests.Fixtures;

/// <summary>
/// Web application factory for integration tests of the Annual Dashboard page.
/// Configures test authentication, mocks IMediator for GetAnnualDashboardQuery,
/// and stubs external HTTP calls.
/// </summary>
public sealed class AnnualWebApplicationFactory : WebApplicationFactory<Program>
{
    private GetAnnualDashboardResultDto? _dashboardResult;

    public AnnualWebApplicationFactory WithDashboardResult(GetAnnualDashboardResultDto result)
    {
        _dashboardResult = result;
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

            // Mock the mediator so the dashboard handler is bypassed.
            services.AddSingleton(_ =>
            {
                Mock<IMediator> mediator = new();

                mediator
                    .Setup(m => m.Send(
                        It.IsAny<GetAnnualDashboardQuery>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(_dashboardResult ?? CreateEmptyResult());

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

    private static GetAnnualDashboardResultDto CreateEmptyResult()
    {
        return new GetAnnualDashboardResultDto(
            Year: DateTime.UtcNow.Year,
            Rows: Array.Empty<AnnualAnalysisRowDto>(),
            AnalysisSummary: new AnnualAnalysisSummaryDto(0m, 0m, 0m, 0m, 0m, 0m, 0m, "EUR"),
            ExecutiveSummary: new AnnualDashboardSummaryDto(
                Income: 0m, Expense: 0m, Net: 0m, Savings: 0m, SavingsRate: 0m,
                Year: DateTime.UtcNow.Year,
                HasPreviousYear: false, HasNextYear: false,
                YearRank: null, TotalYears: 0,
                PreviousYearIncome: null, PreviousYearExpense: null,
                PreviousYearNet: null, PreviousYearSavings: null, PreviousYearSavingsRate: null,
                IncomeChangeAbs: null, IncomeChangePct: null,
                ExpenseChangeAbs: null, ExpenseChangePct: null,
                NetChangeAbs: null, NetChangePct: null,
                SavingsChangeAbs: null, SavingsChangePct: null,
                AverageIncome: null, AverageExpense: null,
                AverageNet: null, AverageSavings: null, AverageSavingsRate: null),
            Ratios: null,
            AvailableYears: Array.Empty<int>(),
            HealthScore: null,
            SmartSummary: "Sin datos para este año. Añade transacciones para ver el resumen anual.",
            HasData: false,
            Currency: "EUR",

            // T2 — all null (empty result)
            MultiYear: null,
            MonthlyEvolution: null,
            Categories: null,
            CategoryTable: null,
            Timeline: null,
            TopExpenses: null,
            TopIncomes: null,
            MostFrequent: null,

            // T3 — empty result
            Anomalies: null,
            Discoveries: null,
            Achievements: null,
            Trends: null,
            Predictions: null,
            HistoricalComparison: null);
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

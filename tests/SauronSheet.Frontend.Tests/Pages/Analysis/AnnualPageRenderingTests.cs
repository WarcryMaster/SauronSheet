using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Frontend.Tests.Fixtures;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Analysis;

/// <summary>
/// Integration tests that render the /Analysis/Annual Razor Page and assert on the
/// generated HTML structure. These tests guide the UI rewrite (T-ANN-003 / T-ANN-004).
/// </summary>
public class AnnualPageRenderingTests
{
    private static HttpClient CreateClient(AnnualWebApplicationFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithData_RendersKpiCardsAndCharts()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory().WithAnalysisResult(CreateSampleResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-kpi-income\"", html);
        Assert.Contains("data-testid=\"annual-kpi-expense\"", html);
        Assert.Contains("data-testid=\"annual-kpi-net\"", html);
        Assert.Contains("data-testid=\"annual-kpi-fixed-pct\"", html);
        Assert.Contains("data-testid=\"annual-trend-chart\"", html);
        Assert.Contains("data-testid=\"annual-distribution-chart\"", html);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithVariation_RendersYoYSection()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory().WithAnalysisResult(CreateSampleResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-yoy-section\"", html);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithData_RendersDetailToggleAndTables()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory().WithAnalysisResult(CreateSampleResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-detail-toggle\"", html);
        Assert.Contains("data-testid=\"annual-income-table\"", html);
        Assert.Contains("data-testid=\"annual-expense-table\"", html);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithoutData_RendersEmptyState()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory().WithAnalysisResult(CreateEmptyResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-empty-state\"", html);
        Assert.DoesNotContain("data-testid=\"annual-kpi-income\"", html);
    }

    private static AnnualAnalysisResultDto CreateSampleResult()
    {
        IReadOnlyList<AnnualAnalysisRowDto> rows = new AnnualAnalysisRowDto[]
        {
            new AnnualAnalysisRowDto(
                Movement: "Salary",
                LineType: AnalysisLineType.IncomeFixed,
                TypeLabel: "Ingreso Fijo",
                Average: 650m,
                MonthlyAmounts: new[]
                {
                    100m, 200m, 300m, 400m, 500m, 600m,
                    700m, 800m, 900m, 1000m, 1100m, 1200m
                },
                Currency: "EUR"),
            new AnnualAnalysisRowDto(
                Movement: "Rent",
                LineType: AnalysisLineType.ExpenseFixed,
                TypeLabel: "Gasto Fijo",
                Average: 60m,
                MonthlyAmounts: Enumerable.Repeat(60m, 12).ToArray(),
                Currency: "EUR")
        };

        AnnualAnalysisSummaryDto summary = new AnnualAnalysisSummaryDto(
            IncomeFixed: 7800m,
            IncomeVariable: 0m,
            IncomeTotal: 7800m,
            ExpenseFixed: 720m,
            ExpenseVariable: 0m,
            ExpenseTotal: 720m,
            Net: 7080m,
            Currency: "EUR")
        {
            Variation = new YearOverYearVariationDto(
                IncomeFixedPct: 5.0m,
                IncomeVariablePct: null,
                IncomeTotalPct: 5.0m,
                ExpenseFixedPct: -2.0m,
                ExpenseVariablePct: null,
                ExpenseTotalPct: -2.0m,
                NetPct: 7.0m,
                HasPreviousYearData: true)
        };

        return new AnnualAnalysisResultDto(
            Year: 2026,
            Rows: rows,
            Summary: summary,
            HasData: true,
            Currency: "EUR");
    }

    private static AnnualAnalysisResultDto CreateEmptyResult()
    {
        return new AnnualAnalysisResultDto(
            Year: 2026,
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
}

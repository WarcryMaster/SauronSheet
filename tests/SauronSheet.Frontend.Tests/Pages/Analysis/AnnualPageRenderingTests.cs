using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Frontend.Tests.Fixtures;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Analysis;

/// <summary>
/// Integration tests that render the /Analysis/Annual Razor Page and assert on the
/// generated HTML structure for the new Annual Dashboard.
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
    public async Task AnnualPage_WithData_RendersDashboardSections()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory()
            .WithDashboardResult(CreateSampleResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-kpi-income\"", html);
        Assert.Contains("data-testid=\"annual-kpi-expense\"", html);
        Assert.Contains("data-testid=\"annual-kpi-net\"", html);
        Assert.Contains("data-testid=\"annual-kpi-savings-rate\"", html);
        Assert.Contains("data-testid=\"annual-smart-summary\"", html);
        Assert.Contains("data-testid=\"annual-health-score\"", html);
        Assert.Contains("data-testid=\"annual-trend-chart\"", html);
        Assert.Contains("data-testid=\"annual-distribution-chart\"", html);
        Assert.Contains("data-testid=\"annual-anomalies-section\"", html);
        Assert.Contains("data-testid=\"annual-discoveries-section\"", html);
        Assert.Contains("data-testid=\"annual-achievements-section\"", html);
        Assert.Contains("data-testid=\"annual-trends-section\"", html);
        Assert.Contains("data-testid=\"annual-predictions-section\"", html);
        Assert.Contains("data-testid=\"annual-historical-comparison-section\"", html);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithData_RendersDetailToggleAndTables()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory()
            .WithDashboardResult(CreateSampleResult());
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
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory()
            .WithDashboardResult(CreateEmptyResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-empty-state\"", html);
        Assert.DoesNotContain("data-testid=\"annual-kpi-income\"", html);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithData_RendersRatios()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory()
            .WithDashboardResult(CreateSampleResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-ratio-savings-rate\"", html);
        Assert.Contains("data-testid=\"annual-ratio-avg-monthly-income\"", html);
        Assert.Contains("data-testid=\"annual-ratio-avg-monthly-expense\"", html);
        Assert.Contains("data-testid=\"annual-ratio-transaction-count\"", html);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithHealthScore_RendersSubScores()
    {
        // Arrange
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory()
            .WithDashboardResult(CreateSampleResult());
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"annual-health-sub-scores\"", html);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    [Trait("Category", "Integration")]
    public async Task AnnualPage_WithYearGaps_RendersPreviousAndNextWithRealAvailableYears()
    {
        // Arrange
        GetAnnualDashboardResultDto result = CreateSampleResult() with
        {
            Year = 2024,
            AvailableYears = new[] { 2022, 2024, 2027 }
        };
        AnnualWebApplicationFactory factory = new AnnualWebApplicationFactory()
            .WithDashboardResult(result);
        HttpClient client = CreateClient(factory);

        // Act
        HttpResponseMessage response = await client.GetAsync("/Analysis/Annual?Year=2024");
        string html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("hx-get=\"/Analysis/Annual?Year=2022\"", html);
        Assert.Contains("hx-get=\"/Analysis/Annual?Year=2027\"", html);
        Assert.DoesNotContain("hx-get=\"/Analysis/Annual?Year=2023\"", html);
        Assert.DoesNotContain("hx-get=\"/Analysis/Annual?Year=2025\"", html);
    }

    private static GetAnnualDashboardResultDto CreateSampleResult()
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

        AnnualAnalysisSummaryDto analysisSummary = new AnnualAnalysisSummaryDto(
            IncomeFixed: 7800m, IncomeVariable: 0m, IncomeTotal: 7800m,
            ExpenseFixed: 720m, ExpenseVariable: 0m, ExpenseTotal: 720m,
            Net: 7080m, Currency: "EUR")
        {
            MonthsWithData = 12
        };

        AnnualDashboardSummaryDto execSummary = new(
            Income: 7800m, Expense: 720m, Net: 7080m, Savings: 7080m, SavingsRate: 90.77m,
            Year: 2026, HasPreviousYear: true, HasNextYear: false,
            YearRank: 1, TotalYears: 3,
            PreviousYearIncome: 7000m, PreviousYearExpense: 800m,
            PreviousYearNet: 6200m, PreviousYearSavings: 6200m, PreviousYearSavingsRate: 88.57m,
            IncomeChangeAbs: 800m, IncomeChangePct: 11.43m,
            ExpenseChangeAbs: -80m, ExpenseChangePct: -10m,
            NetChangeAbs: 880m, NetChangePct: 14.19m,
            SavingsChangeAbs: 880m, SavingsChangePct: 14.19m,
            AverageIncome: 7000m, AverageExpense: 800m,
            AverageNet: 6200m, AverageSavings: 6200m, AverageSavingsRate: 88.57m);

        AnnualDashboardRatiosDto ratios = new(
            SavingsRate: 90.77m, AverageMonthlyIncome: 650m, AverageMonthlyExpense: 60m,
            AverageMonthlySavings: 590m, AverageDailyExpense: 1.97m, AveragePerTransaction: 4260m,
            TransactionCount: 2, AverageOperationsPerMonth: 0.17m);

        AnnualDashboardHealthScoreDto healthScore = new(
            Total: 85m,
            SavingsScore: 100m, IncomeStabilityScore: 80m, ExpenseStabilityScore: 90m,
            CategoryDependencyScore: 70m, BalanceScore: 90m, TrendScore: 75m,
            SavingsWeight: 0.25m, IncomeStabilityWeight: 0.15m,
            ExpenseStabilityWeight: 0.15m, CategoryDependencyWeight: 0.10m,
            BalanceWeight: 0.20m, TrendWeight: 0.15m);

        return new GetAnnualDashboardResultDto(
            Year: 2026,
            Rows: rows,
            AnalysisSummary: analysisSummary,
            ExecutiveSummary: execSummary,
            Ratios: ratios,
            HealthScore: healthScore,
            SmartSummary: "Tus ingresos crecieron un 11.4% respecto al año anterior. Tu tasa de ahorro fue del 90.8%, un nivel excelente.",
            HasData: true,
            Currency: "EUR",
            AvailableYears: Array.Empty<int>(),

            // T2 — all null
            MultiYear: null,
            MonthlyEvolution: null,
            Categories: null,
            CategoryTable: null,
            Timeline: null,
            TopExpenses: null,
            TopIncomes: null,
            MostFrequent: null,

            // T3 sample data
            Anomalies: new[]
            {
                new AnomalyDto("Food", 8, 450m, 120m, 80m, "anomaly", "August anomaly")
            },
            Discoveries: new[]
            {
                new DiscoveryDto("💡", "Top categories concentration", "56% in top 2 categories.", "category-concentration"),
                new DiscoveryDto("🗓️", "Highest spending month", "August highest.", "monthly-pattern"),
                new DiscoveryDto("📅", "Weekday spending pattern", "Monday highest.", "weekday-pattern")
            },
            Achievements: new[]
            {
                new AchievementDto("best-year", "Best Year", "Highest net.", "🏆", true)
            },
            Trends: new[]
            {
                new TrendDto("Food", "growing", 15m, "↑")
            },
            Predictions: new PredictionDto(8000m, 1000m, 7000m, 7000m, 0.9m, 2, true, "ok"),
            HistoricalComparison: new HistoricalComparisonDto(
                Income: new HistoricalComparisonMetricDto(7800m, 7000m, 800m, 11.43m, 7000m, 800m, 11.43m, 8000m, -200m, -2.50m, 6500m, 1300m, 20m),
                Expense: new HistoricalComparisonMetricDto(720m, 800m, -80m, -10m, 760m, -40m, -5.26m, 900m, -180m, -20m, 650m, 70m, 10.77m),
                Savings: new HistoricalComparisonMetricDto(7080m, 6200m, 880m, 14.19m, 6240m, 840m, 13.46m, 7100m, -20m, -0.28m, 5800m, 1280m, 22.07m),
                SavingsRate: new HistoricalComparisonMetricDto(90.77m, 88.57m, 2.2m, 2.48m, 88m, 2.77m, 3.15m, 91m, -0.23m, -0.25m, 82m, 8.77m, 10.70m),
                Balance: new HistoricalComparisonMetricDto(7080m, 6200m, 880m, 14.19m, 6240m, 840m, 13.46m, 7100m, -20m, -0.28m, 5800m, 1280m, 22.07m),
                Message: null));
    }

    private static GetAnnualDashboardResultDto CreateEmptyResult()
    {
        return new GetAnnualDashboardResultDto(
            Year: 2026,
            Rows: Array.Empty<AnnualAnalysisRowDto>(),
            AnalysisSummary: new AnnualAnalysisSummaryDto(0m, 0m, 0m, 0m, 0m, 0m, 0m, "EUR"),
            ExecutiveSummary: new AnnualDashboardSummaryDto(
                Income: 0m, Expense: 0m, Net: 0m, Savings: 0m, SavingsRate: 0m,
                Year: 2026, HasPreviousYear: false, HasNextYear: false,
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
            HealthScore: null,
            SmartSummary: "Sin datos para este año.",
            HasData: false,
            Currency: "EUR",
            AvailableYears: Array.Empty<int>(),

            // T2 — all null
            MultiYear: null,
            MonthlyEvolution: null,
            Categories: null,
            CategoryTable: null,
            Timeline: null,
            TopExpenses: null,
            TopIncomes: null,
            MostFrequent: null,

            // T3 — empty
            Anomalies: null,
            Discoveries: null,
            Achievements: null,
            Trends: null,
            Predictions: null,
            HistoricalComparison: null);
    }
}

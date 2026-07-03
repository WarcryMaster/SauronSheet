namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Application.Resources;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using Xunit;

[Trait("Category", "Application")]
public class InsightsServiceTests
{
    private static readonly UserId TestUserId = new("test-user");

    private static Transaction CreateTransaction(decimal amount, DateTime date, string description = "Test")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            TestUserId,
            new Money(amount, "EUR"),
            date,
            description);
    }

    private static InsightsService CreateService()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        ServiceProvider provider = services.BuildServiceProvider();
        IStringLocalizer<SharedResources> localizer = provider.GetRequiredService<IStringLocalizer<SharedResources>>();
        return new InsightsService(localizer);
    }

    private static IDisposable SetCurrentUiCulture(string cultureName)
    {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo originalCultureFormatting = CultureInfo.CurrentCulture;
        CultureInfo target = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = target;
        CultureInfo.CurrentCulture = target;
        return new CultureRestorer(originalCultureFormatting, originalCulture);
    }

    [Fact]
    public void GenerateSmartSummary_EmptyYear_ReturnsLocalizedNoDataMessageInSpanish()
    {
        InsightsService service = CreateService();
        List<Transaction> transactions = new();
        AnnualDashboardSummaryDto summary = CreateSummary(
            income: 0m,
            expense: 0m,
            savingsRate: 0m,
            hasPreviousYear: false,
            incomeChangePct: null,
            expenseChangePct: null);
        AnnualDashboardRatiosDto ratios = new(null, null, null, null, null, null, 0, null);

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        string result = service.GenerateSmartSummary(transactions, summary, ratios, new List<AnnualAnalysisRowDto>());

        Assert.Contains("Sin datos para este año", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateSmartSummary_IncomeGrew_ReturnsNaturalEnglishNarrative()
    {
        InsightsService service = CreateService();
        List<Transaction> transactions = new()
        {
            CreateTransaction(6000m, new DateTime(2026, 6, 15), "Income")
        };

        AnnualDashboardSummaryDto summary = CreateSummary(
            income: 6000m,
            expense: 2000m,
            savingsRate: 66.7m,
            hasPreviousYear: true,
            incomeChangePct: 20m,
            expenseChangePct: -18m);

        AnnualDashboardRatiosDto ratios = new(66.7m, 6000m, 2000m, 4000m, 5.48m, 4000m, 1, 0.08m);
        List<AnnualAnalysisRowDto> categories = CreateExpenseRows();

        using IDisposable _ = SetCurrentUiCulture("en-US");
        string result = service.GenerateSmartSummary(transactions, summary, ratios, categories);

        Assert.Contains("Your income increased by 20.0% compared to last year.", result, StringComparison.Ordinal);
        Assert.Contains("Your savings rate was 66.7%", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Tus ingresos", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateSmartSummary_IncomeDecreased_ReturnsNaturalSpanishNarrative()
    {
        InsightsService service = CreateService();
        List<Transaction> transactions = new()
        {
            CreateTransaction(4500m, new DateTime(2026, 6, 15), "Income")
        };

        AnnualDashboardSummaryDto summary = CreateSummary(
            income: 4500m,
            expense: 3000m,
            savingsRate: 33.3m,
            hasPreviousYear: true,
            incomeChangePct: -10m,
            expenseChangePct: 20m);

        AnnualDashboardRatiosDto ratios = new(33.3m, 4500m, 3000m, 1500m, 8.22m, 3750m, 2, 0.17m);

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        string result = service.GenerateSmartSummary(transactions, summary, ratios, CreateExpenseRows());

        Assert.Contains("Tus ingresos disminuyeron un 10.0%", result, StringComparison.Ordinal);
        Assert.Contains("Tu tasa de ahorro fue del", result, StringComparison.Ordinal);
        Assert.Contains("%", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Your income", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateDiscoveries_WithInsufficientData_ReturnsLocalizedFallbackInEnglish()
    {
        InsightsService service = CreateService();
        List<Transaction> transactions = new()
        {
            CreateTransaction(-10m, new DateTime(2026, 1, 1), "Coffee")
        };

        using IDisposable _ = SetCurrentUiCulture("en-US");
        IReadOnlyList<DiscoveryDto> discoveries = service.GenerateDiscoveries(transactions);

        Assert.Single(discoveries);
        Assert.Equal("No discoveries", discoveries[0].Title);
        Assert.Equal("Not enough data to generate discoveries.", discoveries[0].Description);
    }

    [Fact]
    public void GenerateDiscoveries_WithRichData_ReturnsSpanishDiscoveryTitles()
    {
        InsightsService service = CreateService();
        List<Transaction> transactions = new()
        {
            CreateTransaction(-300m, new DateTime(2026, 8, 4), "Groceries"),
            CreateTransaction(-280m, new DateTime(2026, 8, 11), "Groceries"),
            CreateTransaction(-260m, new DateTime(2026, 8, 18), "Groceries"),
            CreateTransaction(-220m, new DateTime(2026, 1, 6), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 2, 3), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 3, 3), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 4, 7), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 5, 5), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 6, 2), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 7, 7), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 9, 1), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 10, 6), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 11, 3), "Rent"),
            CreateTransaction(-220m, new DateTime(2026, 12, 1), "Rent")
        };

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        IReadOnlyList<DiscoveryDto> discoveries = service.GenerateDiscoveries(transactions);

        Assert.True(discoveries.Count >= 3);
        Assert.Contains(discoveries, d => d.Title == "Concentración en categorías principales");
        Assert.Contains(discoveries, d => d.Title == "Mes con mayor gasto");
        Assert.Contains(discoveries, d => d.Title == "Patrón de gasto por día de la semana");
    }

    private static AnnualDashboardSummaryDto CreateSummary(
        decimal income,
        decimal expense,
        decimal savingsRate,
        bool hasPreviousYear,
        decimal? incomeChangePct,
        decimal? expenseChangePct)
    {
        decimal net = income - expense;
        return new AnnualDashboardSummaryDto(
            Income: income,
            Expense: expense,
            Net: net,
            Savings: net,
            SavingsRate: savingsRate,
            Year: 2026,
            HasPreviousYear: hasPreviousYear,
            HasNextYear: false,
            YearRank: 1,
            TotalYears: 3,
            PreviousYearIncome: hasPreviousYear ? income - 1000m : null,
            PreviousYearExpense: hasPreviousYear ? expense + 100m : null,
            PreviousYearNet: hasPreviousYear ? net - 1100m : null,
            PreviousYearSavings: hasPreviousYear ? net - 1100m : null,
            PreviousYearSavingsRate: hasPreviousYear ? savingsRate - 5m : null,
            IncomeChangeAbs: null,
            IncomeChangePct: incomeChangePct,
            ExpenseChangeAbs: null,
            ExpenseChangePct: expenseChangePct,
            NetChangeAbs: null,
            NetChangePct: null,
            SavingsChangeAbs: null,
            SavingsChangePct: null,
            AverageIncome: null,
            AverageExpense: null,
            AverageNet: null,
            AverageSavings: null,
            AverageSavingsRate: null);
    }

    private static List<AnnualAnalysisRowDto> CreateExpenseRows()
    {
        return new List<AnnualAnalysisRowDto>
        {
            new("Rent", AnalysisLineType.ExpenseFixed, "Fixed Expense", 100m, new[] { 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m }, "EUR"),
            new("Food", AnalysisLineType.ExpenseVariable, "Variable Expense", 75m, new[] { 75m, 75m, 75m, 75m, 75m, 75m, 75m, 75m, 75m, 75m, 75m, 75m }, "EUR")
        };
    }

    private sealed class CultureRestorer : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUiCulture;

        public CultureRestorer(CultureInfo originalCulture, CultureInfo originalUiCulture)
        {
            _originalCulture = originalCulture;
            _originalUiCulture = originalUiCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}

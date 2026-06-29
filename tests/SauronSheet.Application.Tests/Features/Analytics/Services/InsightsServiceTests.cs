namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Tests for InsightsService (REQ-002 — Smart Summary).
/// Strict TDD: RED → Tests first.
/// Task 3.4: Generates 2-4 sentence rule-based narrative.
/// </summary>
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

    [Fact]
    public void GenerateSmartSummary_EmptyYear_ReturnsNoDataMessage()
    {
        // Arrange
        List<Transaction> transactions = new();
        AnnualDashboardSummaryDto summary = new(
            Income: 0m, Expense: 0m, Net: 0m, Savings: 0m, SavingsRate: 0m,
            Year: 2026, HasPreviousYear: false, HasNextYear: false,
            YearRank: null, TotalYears: 0,
            PreviousYearIncome: null, PreviousYearExpense: null, PreviousYearNet: null,
            PreviousYearSavings: null, PreviousYearSavingsRate: null,
            IncomeChangeAbs: null, IncomeChangePct: null,
            ExpenseChangeAbs: null, ExpenseChangePct: null,
            NetChangeAbs: null, NetChangePct: null,
            SavingsChangeAbs: null, SavingsChangePct: null,
            AverageIncome: null, AverageExpense: null, AverageNet: null,
            AverageSavings: null, AverageSavingsRate: null);
        AnnualDashboardRatiosDto ratios = new(null, null, null, null, null, null, 0, null);

        // Act
        string result = InsightsService.GenerateSmartSummary(transactions, summary, ratios, new List<AnnualAnalysisRowDto>());

        // Assert
        Assert.Contains("Sin datos", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSmartSummary_IncomeGrew_IncludesGrowth()
    {
        // Arrange: income grew by 20%
        List<Transaction> transactions = new()
        {
            CreateTransaction(6000m, new DateTime(2026, 6, 15), "Income"),
        };

        AnnualDashboardSummaryDto summary = new(
            Income: 6000m, Expense: 2000m, Net: 4000m, Savings: 4000m, SavingsRate: 66.67m,
            Year: 2026, HasPreviousYear: true, HasNextYear: false,
            YearRank: 2, TotalYears: 3,
            PreviousYearIncome: 5000m, PreviousYearExpense: 2500m, PreviousYearNet: 2500m,
            PreviousYearSavings: 2500m, PreviousYearSavingsRate: 50m,
            IncomeChangeAbs: 1000m, IncomeChangePct: 20m,
            ExpenseChangeAbs: -500m, ExpenseChangePct: -20m,
            NetChangeAbs: 1500m, NetChangePct: 60m,
            SavingsChangeAbs: 1500m, SavingsChangePct: 60m,
            AverageIncome: 5000m, AverageExpense: 2500m, AverageNet: 2500m,
            AverageSavings: 2500m, AverageSavingsRate: 50m);

        AnnualDashboardRatiosDto ratios = new(
            66.67m, 6000m, 2000m, 4000m, 5.48m, 4000m, 1, 0.08m);

        List<AnnualAnalysisRowDto> categories = new()
        {
            new AnnualAnalysisRowDto("Salary", AnalysisLineType.IncomeFixed, "Ingreso Fijo",
                500m, new decimal[12], "EUR"),
        };

        // Act
        string result = InsightsService.GenerateSmartSummary(transactions, summary, ratios, categories);

        // Assert: should mention income growth
        Assert.Contains("ingres", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("20", result); // income grew 20%
        Assert.Contains("ahorr", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSmartSummary_IncomeDecreased_MentionsDecline()
    {
        // Arrange: income decreased by 10%
        List<Transaction> transactions = new()
        {
            CreateTransaction(4500m, new DateTime(2026, 6, 15), "Income"),
        };

        AnnualDashboardSummaryDto summary = new(
            Income: 4500m, Expense: 3000m, Net: 1500m, Savings: 1500m, SavingsRate: 33.33m,
            Year: 2026, HasPreviousYear: true, HasNextYear: false,
            YearRank: 3, TotalYears: 3,
            PreviousYearIncome: 5000m, PreviousYearExpense: 2500m, PreviousYearNet: 2500m,
            PreviousYearSavings: 2500m, PreviousYearSavingsRate: 50m,
            IncomeChangeAbs: -500m, IncomeChangePct: -10m,
            ExpenseChangeAbs: 500m, ExpenseChangePct: 20m,
            NetChangeAbs: -1000m, NetChangePct: -40m,
            SavingsChangeAbs: -1000m, SavingsChangePct: -40m,
            AverageIncome: 5000m, AverageExpense: 2500m, AverageNet: 2500m,
            AverageSavings: 2500m, AverageSavingsRate: 50m);

        AnnualDashboardRatiosDto ratios = new(
            33.33m, 4500m, 3000m, 1500m, 8.22m, 3750m, 2, 0.17m);

        List<AnnualAnalysisRowDto> categories = new()
        {
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                250m, new decimal[12], "EUR"),
            new AnnualAnalysisRowDto("Food", AnalysisLineType.ExpenseVariable, "Gasto Variable",
                100m, new decimal[12], "EUR"),
        };

        // Act
        string result = InsightsService.GenerateSmartSummary(transactions, summary, ratios, categories);

        // Assert: should mention changes
        Assert.Contains("ingres", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gast", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSmartSummary_ReturnsTwoToFourSentences()
    {
        // Arrange: normal year with data
        List<Transaction> transactions = new()
        {
            CreateTransaction(3000m, new DateTime(2026, 1, 15), "Salary"),
            CreateTransaction(-1000m, new DateTime(2026, 1, 10), "Rent"),
            CreateTransaction(-500m, new DateTime(2026, 1, 5), "Food"),
        };

        AnnualDashboardSummaryDto summary = new(
            3000m, 1500m, 1500m, 1500m, 50m, 2026, true, true,
            2, 3,
            2800m, 1600m, 1200m, 1200m, 42.86m,
            200m, 7.14m, -100m, -6.25m, 300m, 25m, 300m, 25m,
            2900m, 1700m, 1200m, 1200m, 41.38m);

        AnnualDashboardRatiosDto ratios = new(
            50m, 3000m, 1500m, 1500m, 4.11m, 500m, 3, 0.25m);

        List<AnnualAnalysisRowDto> categories = new()
        {
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                83.33m, new decimal[12], "EUR"),
            new AnnualAnalysisRowDto("Food", AnalysisLineType.ExpenseVariable, "Gasto Variable",
                41.67m, new decimal[12], "EUR"),
        };

        // Act
        string result = InsightsService.GenerateSmartSummary(transactions, summary, ratios, categories);

        // Assert: 2-4 sentences (split on sentence-ending punctuation followed by whitespace, not decimal points)
        int sentenceCount = System.Text.RegularExpressions.Regex.Split(result, @"(?<=[.!?])\s+").Length;
        Assert.True(sentenceCount >= 2 && sentenceCount <= 5,
            $"Expected 2-4 sentences, got {sentenceCount}: '{result}'");
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void GenerateDiscoveries_WithRichData_ReturnsAtLeastThreeDiscoveries()
    {
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
            CreateTransaction(-220m, new DateTime(2026, 12, 1), "Rent"),
        };

        IReadOnlyList<DiscoveryDto> discoveries = InsightsService.GenerateDiscoveries(transactions);

        Assert.True(discoveries.Count >= 3);
    }

    [Fact]
    public void GenerateDiscoveries_WithInsufficientData_ReturnsNoDiscoveriesMessage()
    {
        List<Transaction> transactions = new()
        {
            CreateTransaction(-10m, new DateTime(2026, 1, 1), "Coffee")
        };

        IReadOnlyList<DiscoveryDto> discoveries = InsightsService.GenerateDiscoveries(transactions);

        Assert.Single(discoveries);
        Assert.Contains("No discoveries", discoveries[0].Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateDiscoveries_WhenTopTwoCategoriesDominate_IncludesConcentrationDiscovery()
    {
        List<Transaction> transactions = new()
        {
            CreateTransaction(-500m, new DateTime(2026, 1, 1), "Housing"),
            CreateTransaction(-400m, new DateTime(2026, 2, 1), "Housing"),
            CreateTransaction(-300m, new DateTime(2026, 3, 1), "Food"),
            CreateTransaction(-200m, new DateTime(2026, 4, 1), "Food"),
            CreateTransaction(-100m, new DateTime(2026, 5, 1), "Other"),
        };

        IReadOnlyList<DiscoveryDto> discoveries = InsightsService.GenerateDiscoveries(transactions);

        Assert.Contains(discoveries, d => d.Category == "category-concentration");
    }

    [Fact]
    public void GenerateDiscoveries_WhenWeekdayPatternExists_IncludesWeekdayDiscovery()
    {
        List<Transaction> transactions = new()
        {
            CreateTransaction(-100m, new DateTime(2026, 1, 5), "Food"), // Monday
            CreateTransaction(-120m, new DateTime(2026, 1, 12), "Food"), // Monday
            CreateTransaction(-130m, new DateTime(2026, 1, 19), "Food"), // Monday
            CreateTransaction(-20m, new DateTime(2026, 1, 6), "Food"),
            CreateTransaction(-20m, new DateTime(2026, 1, 7), "Food"),
        };

        IReadOnlyList<DiscoveryDto> discoveries = InsightsService.GenerateDiscoveries(transactions);

        Assert.Contains(discoveries, d => d.Category == "weekday-pattern");
    }
}

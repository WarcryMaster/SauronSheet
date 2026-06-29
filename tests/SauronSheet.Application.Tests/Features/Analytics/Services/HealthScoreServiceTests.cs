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
/// Tests for HealthScoreService (REQ-012).
/// Strict TDD: RED → Tests first.
/// Task 3.3: 6 sub-scores with weights → total 0-100.
/// </summary>
[Trait("Category", "Application")]
public class HealthScoreServiceTests
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

    private static AnnualDashboardSummaryDto EmptySummary => new(
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

    private static AnnualDashboardRatiosDto EmptyRatios => new(
        SavingsRate: null, AverageMonthlyIncome: null, AverageMonthlyExpense: null,
        AverageMonthlySavings: null, AverageDailyExpense: null, AveragePerTransaction: null,
        TransactionCount: 0, AverageOperationsPerMonth: null);

    [Fact]
    public void Compute_EmptyTransactions_AllScoresNull()
    {
        // Arrange
        List<Transaction> transactions = new();

        // Act
        AnnualDashboardHealthScoreDto result = HealthScoreService.Compute(
            transactions, EmptySummary, EmptyRatios,
            new List<AnnualAnalysisRowDto>());

        // Assert
        Assert.Null(result.Total);
        Assert.Null(result.SavingsScore);
        Assert.Equal(0.25m, result.SavingsWeight);
        Assert.Equal(0.15m, result.IncomeStabilityWeight);
        Assert.Equal(0.15m, result.ExpenseStabilityWeight);
        Assert.Equal(0.10m, result.CategoryDependencyWeight);
        Assert.Equal(0.20m, result.BalanceWeight);
        Assert.Equal(0.15m, result.TrendWeight);
    }

    [Fact]
    public void Compute_PerfectHealth_Returns100()
    {
        // Arrange: ideal financial scenario
        List<Transaction> transactions = new();
        for (int month = 1; month <= 12; month++)
        {
            transactions.Add(CreateTransaction(5000m, new DateTime(2026, month, 15), $"Income {month}"));
            transactions.Add(CreateTransaction(-1000m, new DateTime(2026, month, 10), $"Expense {month}"));
        }

        AnnualDashboardSummaryDto perfectSummary = new(
            Income: 60000m, Expense: 12000m, Net: 48000m, Savings: 48000m, SavingsRate: 80m,
            Year: 2026, HasPreviousYear: true, HasNextYear: false,
            YearRank: 1, TotalYears: 5,
            PreviousYearIncome: 50000m, PreviousYearExpense: 13000m, PreviousYearNet: 37000m,
            PreviousYearSavings: 37000m, PreviousYearSavingsRate: 74m,
            IncomeChangeAbs: 10000m, IncomeChangePct: 20m,
            ExpenseChangeAbs: -1000m, ExpenseChangePct: -7.69m,
            NetChangeAbs: 11000m, NetChangePct: 29.73m,
            SavingsChangeAbs: 11000m, SavingsChangePct: 29.73m,
            AverageIncome: 50000m, AverageExpense: 15000m, AverageNet: 35000m,
            AverageSavings: 35000m, AverageSavingsRate: 70m);

        AnnualDashboardRatiosDto healthyRatios = new(
            SavingsRate: 80m, AverageMonthlyIncome: 5000m, AverageMonthlyExpense: 1000m,
            AverageMonthlySavings: 4000m, AverageDailyExpense: 32.88m, AveragePerTransaction: 3000m,
            TransactionCount: 24, AverageOperationsPerMonth: 2m);

        List<AnnualAnalysisRowDto> diversifiedCategories = new()
        {
            new AnnualAnalysisRowDto("Salary", AnalysisLineType.IncomeFixed, "Ingreso Fijo",
                4166.67m, new decimal[12], "EUR"),
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                1000m, new decimal[12], "EUR"),
            new AnnualAnalysisRowDto("Food", AnalysisLineType.ExpenseVariable, "Gasto Variable",
                500m, new decimal[12], "EUR"),
        };

        // Act
        AnnualDashboardHealthScoreDto result = HealthScoreService.Compute(
            transactions, perfectSummary, healthyRatios, diversifiedCategories);

        // Assert
        Assert.NotNull(result.Total);
        Assert.True(result.Total > 80m, $"Expected score > 80, got {result.Total}");
        Assert.True(result.SavingsScore >= 80m);
        Assert.True(result.IncomeStabilityScore >= 80m);
        Assert.True(result.ExpenseStabilityScore >= 80m);
    }

    [Fact]
    public void Compute_PoorHealth_ReturnsLowScore()
    {
        // Arrange: very poor financial health
        List<Transaction> transactions = new();
        for (int month = 1; month <= 3; month++)
        {
            transactions.Add(CreateTransaction(-1000m, new DateTime(2026, month, 10), $"Expense {month}"));
        }

        AnnualDashboardSummaryDto poorSummary = new(
            Income: 0m, Expense: 3000m, Net: -3000m, Savings: 0m, SavingsRate: 0m,
            Year: 2026, HasPreviousYear: false, HasNextYear: true,
            YearRank: null, TotalYears: 1,
            PreviousYearIncome: null, PreviousYearExpense: null, PreviousYearNet: null,
            PreviousYearSavings: null, PreviousYearSavingsRate: null,
            IncomeChangeAbs: null, IncomeChangePct: null,
            ExpenseChangeAbs: null, ExpenseChangePct: null,
            NetChangeAbs: null, NetChangePct: null,
            SavingsChangeAbs: null, SavingsChangePct: null,
            AverageIncome: null, AverageExpense: null, AverageNet: null,
            AverageSavings: null, AverageSavingsRate: null);

        AnnualDashboardRatiosDto poorRatios = new(
            SavingsRate: null, AverageMonthlyIncome: null, AverageMonthlyExpense: 1000m,
            AverageMonthlySavings: -1000m, AverageDailyExpense: 8.22m, AveragePerTransaction: 1000m,
            TransactionCount: 3, AverageOperationsPerMonth: 0.25m);

        List<AnnualAnalysisRowDto> concentratedCategories = new()
        {
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                1000m, new decimal[12], "EUR"),
        };

        // Act
        AnnualDashboardHealthScoreDto result = HealthScoreService.Compute(
            transactions, poorSummary, poorRatios, concentratedCategories);

        // Assert
        Assert.NotNull(result.Total);
        Assert.True(result.Total <= 50m, $"Expected low score <= 50, got {result.Total}");
        Assert.True(result.SavingsScore <= 10m);
    }

    [Fact]
    public void Compute_SubScores_WeightsSumToOne()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(3000m, new DateTime(2026, 1, 15), "Income"),
            CreateTransaction(-1500m, new DateTime(2026, 1, 10), "Expense"),
        };

        AnnualDashboardSummaryDto summary = new(
            Income: 3000m, Expense: 1500m, Net: 1500m, Savings: 1500m, SavingsRate: 50m,
            Year: 2026, HasPreviousYear: false, HasNextYear: false,
            YearRank: null, TotalYears: 1,
            PreviousYearIncome: null, PreviousYearExpense: null, PreviousYearNet: null,
            PreviousYearSavings: null, PreviousYearSavingsRate: null,
            IncomeChangeAbs: null, IncomeChangePct: null,
            ExpenseChangeAbs: null, ExpenseChangePct: null,
            NetChangeAbs: null, NetChangePct: null,
            SavingsChangeAbs: null, SavingsChangePct: null,
            AverageIncome: null, AverageExpense: null, AverageNet: null,
            AverageSavings: null, AverageSavingsRate: null);

        AnnualDashboardRatiosDto ratios = new(
            SavingsRate: 50m, AverageMonthlyIncome: 3000m, AverageMonthlyExpense: 1500m,
            AverageMonthlySavings: 1500m, AverageDailyExpense: 4.11m, AveragePerTransaction: 1500m,
            TransactionCount: 2, AverageOperationsPerMonth: 0.17m);

        List<AnnualAnalysisRowDto> categories = new()
        {
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                1500m, new decimal[12], "EUR"),
        };

        // Act
        AnnualDashboardHealthScoreDto result = HealthScoreService.Compute(
            transactions, summary, ratios, categories);

        // Assert: weights sum to 1.0
        decimal weightSum = result.SavingsWeight + result.IncomeStabilityWeight
            + result.ExpenseStabilityWeight + result.CategoryDependencyWeight
            + result.BalanceWeight + result.TrendWeight;
        Assert.Equal(1.0m, weightSum);

        // Individual scores should be non-null
        Assert.NotNull(result.SavingsScore);
        Assert.NotNull(result.IncomeStabilityScore);
        Assert.NotNull(result.ExpenseStabilityScore);
        Assert.NotNull(result.CategoryDependencyScore);
        Assert.NotNull(result.BalanceScore);
        Assert.NotNull(result.TrendScore);

        // Weighted total should be the sum of (score * weight)
        decimal expectedTotal = Math.Round(
            result.SavingsScore!.Value * result.SavingsWeight +
            result.IncomeStabilityScore!.Value * result.IncomeStabilityWeight +
            result.ExpenseStabilityScore!.Value * result.ExpenseStabilityWeight +
            result.CategoryDependencyScore!.Value * result.CategoryDependencyWeight +
            result.BalanceScore!.Value * result.BalanceWeight +
            result.TrendScore!.Value * result.TrendWeight, 2);
        Assert.Equal(expectedTotal, result.Total!.Value);
    }

    [Fact]
    public void Compute_NoIncome_SavingsScoreZero()
    {
        // Arrange: only expenses
        List<Transaction> transactions = new()
        {
            CreateTransaction(-1000m, new DateTime(2026, 1, 10), "Rent"),
        };

        AnnualDashboardSummaryDto zeroIncomeSummary = new(
            Income: 0m, Expense: 1000m, Net: -1000m, Savings: 0m, SavingsRate: 0m,
            Year: 2026, HasPreviousYear: false, HasNextYear: false,
            YearRank: null, TotalYears: 1,
            PreviousYearIncome: null, PreviousYearExpense: null, PreviousYearNet: null,
            PreviousYearSavings: null, PreviousYearSavingsRate: null,
            IncomeChangeAbs: null, IncomeChangePct: null,
            ExpenseChangeAbs: null, ExpenseChangePct: null,
            NetChangeAbs: null, NetChangePct: null,
            SavingsChangeAbs: null, SavingsChangePct: null,
            AverageIncome: null, AverageExpense: null, AverageNet: null,
            AverageSavings: null, AverageSavingsRate: null);

        AnnualDashboardRatiosDto zeroIncomeRatios = new(
            SavingsRate: null, AverageMonthlyIncome: null, AverageMonthlyExpense: 1000m,
            AverageMonthlySavings: -1000m, AverageDailyExpense: 2.74m, AveragePerTransaction: 1000m,
            TransactionCount: 1, AverageOperationsPerMonth: 0.08m);

        // Act
        AnnualDashboardHealthScoreDto result = HealthScoreService.Compute(
            transactions, zeroIncomeSummary, zeroIncomeRatios, new List<AnnualAnalysisRowDto>());

        // Assert
        Assert.NotNull(result.Total);
        Assert.True(result.Total < 50m, $"Expected low total score, got {result.Total}");
        Assert.Equal(0m, result.SavingsScore); // 0% savings rate → 0
    }
}

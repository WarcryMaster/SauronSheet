namespace SauronSheet.Application.Tests.Features.Analytics.DTOs;

using System;
using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Tests for the new AnnualDashboard DTOs (PR 1 — T1 Core).
/// Strict TDD: RED → Tests first, then implement DTOs.
/// Covers Tasks 2.1 through 2.5.
/// </summary>
[Trait("Category", "Application")]
public class AnnualDashboardDtosTests
{
    // ── Task 2.1: GetAnnualDashboardResultDto construction ──

    [Fact]
    public void GetAnnualDashboardResultDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange
        AnnualDashboardSummaryDto summary = new(
            Income: 50000m,
            Expense: 30000m,
            Net: 20000m,
            Savings: 20000m,
            SavingsRate: 40m,
            Year: 2026,
            HasPreviousYear: true,
            HasNextYear: false,
            YearRank: 1,
            TotalYears: 3,
            PreviousYearIncome: 45000m,
            PreviousYearExpense: 28000m,
            PreviousYearNet: 17000m,
            PreviousYearSavings: 17000m,
            PreviousYearSavingsRate: 37.78m,
            IncomeChangeAbs: 5000m,
            IncomeChangePct: 11.11m,
            ExpenseChangeAbs: 2000m,
            ExpenseChangePct: 7.14m,
            NetChangeAbs: 3000m,
            NetChangePct: 17.65m,
            SavingsChangeAbs: 3000m,
            SavingsChangePct: 17.65m,
            AverageIncome: 40000m,
            AverageExpense: 25000m,
            AverageNet: 15000m,
            AverageSavings: 15000m,
            AverageSavingsRate: 35m);

        AnnualDashboardRatiosDto ratios = new(
            SavingsRate: 40m,
            AverageMonthlyIncome: 4166.67m,
            AverageMonthlyExpense: 2500m,
            AverageMonthlySavings: 1666.67m,
            AverageDailyExpense: 82.19m,
            AveragePerTransaction: 45.50m,
            TransactionCount: 650,
            AverageOperationsPerMonth: 54.17m);

        AnnualDashboardHealthScoreDto healthScore = new(
            Total: 78m,
            SavingsScore: 80m,
            IncomeStabilityScore: 75m,
            ExpenseStabilityScore: 70m,
            CategoryDependencyScore: 85m,
            BalanceScore: 82m,
            TrendScore: 60m,
            SavingsWeight: 0.25m,
            IncomeStabilityWeight: 0.15m,
            ExpenseStabilityWeight: 0.15m,
            CategoryDependencyWeight: 0.10m,
            BalanceWeight: 0.20m,
            TrendWeight: 0.15m);

        // Act
        GetAnnualDashboardResultDto dto = new(
            Year: 2026,
            Rows: Array.Empty<AnnualAnalysisRowDto>(),
            AnalysisSummary: new AnnualAnalysisSummaryDto(0m, 0m, 0m, 0m, 0m, 0m, 0m, "EUR"),
            ExecutiveSummary: summary,
            Ratios: ratios,
            HealthScore: healthScore,
            SmartSummary: "Tus ingresos crecieron un 11.11% respecto al año anterior.",
            HasData: true,
            Currency: "EUR",
            AvailableYears: new List<int> { 2024, 2025, 2026 }.AsReadOnly(),

            // T2 defaults
            MultiYear: null,
            MonthlyEvolution: null,
            Categories: null,
            CategoryTable: null,
            Timeline: null,
            TopExpenses: null,
            TopIncomes: null,
            MostFrequent: null);

        // Assert
        Assert.Equal(2026, dto.Year);
        Assert.True(dto.HasData);
        Assert.NotNull(dto.ExecutiveSummary);
        Assert.NotNull(dto.Ratios);
        Assert.NotNull(dto.HealthScore);
        Assert.Equal(3, dto.AvailableYears.Count);
        Assert.True(dto.ExecutiveSummary.HasPreviousYear);
        Assert.False(dto.ExecutiveSummary.HasNextYear);
    }

    [Fact]
    public void GetAnnualDashboardResultDto_NoData_ReturnsEmptyState()
    {
        // Arrange
        AnnualDashboardSummaryDto emptySummary = new(
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

        // Act
        GetAnnualDashboardResultDto dto = new(
            Year: 2026,
            Rows: Array.Empty<AnnualAnalysisRowDto>(),
            AnalysisSummary: new AnnualAnalysisSummaryDto(0m, 0m, 0m, 0m, 0m, 0m, 0m, "EUR"),
            ExecutiveSummary: emptySummary,
            Ratios: null,
            HealthScore: null,
            SmartSummary: "Sin datos para este año",
            HasData: false,
            Currency: "EUR",
            AvailableYears: Array.Empty<int>(),

            // T2 defaults
            MultiYear: null,
            MonthlyEvolution: null,
            Categories: null,
            CategoryTable: null,
            Timeline: null,
            TopExpenses: null,
            TopIncomes: null,
            MostFrequent: null);

        // Assert
        Assert.False(dto.HasData);
        Assert.Equal("Sin datos para este año", dto.SmartSummary);
        Assert.Empty(dto.AvailableYears);
        Assert.NotNull(dto.ExecutiveSummary);
        Assert.False(dto.ExecutiveSummary.HasPreviousYear);
        Assert.False(dto.ExecutiveSummary.HasNextYear);
    }

    // ── Task 2.3: AnnualDashboardRatiosDto null/div-0 handling ──

    [Fact]
    public void AnnualDashboardRatiosDto_DivisionByZero_ReturnsNulls()
    {
        // Arrange & Act: DTO with zero transaction count — all ratios nullable
        AnnualDashboardRatiosDto dto = new(
            SavingsRate: null, AverageMonthlyIncome: null, AverageMonthlyExpense: null,
            AverageMonthlySavings: null, AverageDailyExpense: null, AveragePerTransaction: null,
            TransactionCount: 0, AverageOperationsPerMonth: null);

        // Assert
        Assert.Null(dto.SavingsRate);
        Assert.Null(dto.AverageMonthlyIncome);
        Assert.Null(dto.AverageMonthlyExpense);
        Assert.Null(dto.AverageDailyExpense);
        Assert.Null(dto.AveragePerTransaction);
        Assert.Equal(0, dto.TransactionCount);
        Assert.Null(dto.AverageOperationsPerMonth);
    }

    [Fact]
    public void AnnualDashboardRatiosDto_WithData_ReturnsComputedRatios()
    {
        // Arrange & Act
        AnnualDashboardRatiosDto dto = new(
            SavingsRate: 40m,
            AverageMonthlyIncome: 4166.67m,
            AverageMonthlyExpense: 2500m,
            AverageMonthlySavings: 1666.67m,
            AverageDailyExpense: 82.19m,
            AveragePerTransaction: 45.50m,
            TransactionCount: 650,
            AverageOperationsPerMonth: 54.17m);

        // Assert
        Assert.Equal(40m, dto.SavingsRate);
        Assert.Equal(4166.67m, dto.AverageMonthlyIncome);
        Assert.Equal(2500m, dto.AverageMonthlyExpense);
        Assert.Equal(82.19m, dto.AverageDailyExpense);
        Assert.Equal(45.50m, dto.AveragePerTransaction);
        Assert.Equal(650, dto.TransactionCount);
        Assert.Equal(54.17m, dto.AverageOperationsPerMonth);
    }

    // ── Task 2.4: AnnualDashboardHealthScoreDto ──

    [Fact]
    public void AnnualDashboardHealthScoreDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange & Act
        AnnualDashboardHealthScoreDto dto = new(
            Total: 78m,
            SavingsScore: 80m,
            IncomeStabilityScore: 75m,
            ExpenseStabilityScore: 70m,
            CategoryDependencyScore: 85m,
            BalanceScore: 82m,
            TrendScore: 60m,
            SavingsWeight: 0.25m,
            IncomeStabilityWeight: 0.15m,
            ExpenseStabilityWeight: 0.15m,
            CategoryDependencyWeight: 0.10m,
            BalanceWeight: 0.20m,
            TrendWeight: 0.15m);

        // Assert
        Assert.Equal(78m, dto.Total);
        Assert.Equal(80m, dto.SavingsScore);
        Assert.Equal(75m, dto.IncomeStabilityScore);
        Assert.Equal(70m, dto.ExpenseStabilityScore);
        Assert.Equal(85m, dto.CategoryDependencyScore);
        Assert.Equal(82m, dto.BalanceScore);
        Assert.Equal(60m, dto.TrendScore);

        // Weights sum to 1.0
        decimal totalWeight = dto.SavingsWeight + dto.IncomeStabilityWeight
            + dto.ExpenseStabilityWeight + dto.CategoryDependencyWeight
            + dto.BalanceWeight + dto.TrendWeight;
        Assert.Equal(1.0m, totalWeight);
    }

    [Fact]
    public void AnnualDashboardHealthScoreDto_NoScore_AllNull()
    {
        // Arrange & Act: No transactions → all scores null
        AnnualDashboardHealthScoreDto dto = new(
            Total: null, SavingsScore: null, IncomeStabilityScore: null,
            ExpenseStabilityScore: null, CategoryDependencyScore: null,
            BalanceScore: null, TrendScore: null,
            SavingsWeight: 0.25m, IncomeStabilityWeight: 0.15m,
            ExpenseStabilityWeight: 0.15m, CategoryDependencyWeight: 0.10m,
            BalanceWeight: 0.20m, TrendWeight: 0.15m);

        // Assert
        Assert.Null(dto.Total);
        Assert.Null(dto.SavingsScore);
        Assert.Null(dto.IncomeStabilityScore);
        Assert.Null(dto.ExpenseStabilityScore);
        Assert.Null(dto.CategoryDependencyScore);
        Assert.Null(dto.BalanceScore);
        Assert.Null(dto.TrendScore);
    }

    // ── Task 2.2: AnnualDashboardSummaryDto ──

    [Fact]
    public void AnnualDashboardSummaryDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange & Act
        AnnualDashboardSummaryDto dto = new(
            Income: 50000m, Expense: 30000m, Net: 20000m,
            Savings: 20000m, SavingsRate: 40m,
            Year: 2026, HasPreviousYear: true, HasNextYear: false,
            YearRank: 1, TotalYears: 3,
            PreviousYearIncome: 45000m, PreviousYearExpense: 28000m,
            PreviousYearNet: 17000m, PreviousYearSavings: 17000m,
            PreviousYearSavingsRate: 37.78m,
            IncomeChangeAbs: 5000m, IncomeChangePct: 11.11m,
            ExpenseChangeAbs: 2000m, ExpenseChangePct: 7.14m,
            NetChangeAbs: 3000m, NetChangePct: 17.65m,
            SavingsChangeAbs: 3000m, SavingsChangePct: 17.65m,
            AverageIncome: 40000m, AverageExpense: 25000m,
            AverageNet: 15000m, AverageSavings: 15000m,
            AverageSavingsRate: 35m);

        // Assert
        Assert.Equal(50000m, dto.Income);
        Assert.Equal(30000m, dto.Expense);
        Assert.Equal(20000m, dto.Net);
        Assert.Equal(20000m, dto.Savings);
        Assert.Equal(40m, dto.SavingsRate);
        Assert.Equal(2026, dto.Year);
        Assert.True(dto.HasPreviousYear);
        Assert.False(dto.HasNextYear);
        Assert.Equal(1, dto.YearRank);
        Assert.Equal(3, dto.TotalYears);
        Assert.Equal(5000m, dto.IncomeChangeAbs);
        Assert.Equal(11.11m, dto.IncomeChangePct);
        Assert.Equal(2000m, dto.ExpenseChangeAbs);
        Assert.Equal(7.14m, dto.ExpenseChangePct);
    }

    [Fact]
    public void AnnualDashboardSummaryDto_NoPreviousYear_NullChanges()
    {
        // Arrange & Act: First year — no previous data
        AnnualDashboardSummaryDto dto = new(
            Income: 30000m, Expense: 20000m, Net: 10000m,
            Savings: 10000m, SavingsRate: 33.33m,
            Year: 2024, HasPreviousYear: false, HasNextYear: true,
            YearRank: null, TotalYears: 1,
            PreviousYearIncome: null, PreviousYearExpense: null,
            PreviousYearNet: null, PreviousYearSavings: null,
            PreviousYearSavingsRate: null,
            IncomeChangeAbs: null, IncomeChangePct: null,
            ExpenseChangeAbs: null, ExpenseChangePct: null,
            NetChangeAbs: null, NetChangePct: null,
            SavingsChangeAbs: null, SavingsChangePct: null,
            AverageIncome: null, AverageExpense: null,
            AverageNet: null, AverageSavings: null,
            AverageSavingsRate: null);

        // Assert
        Assert.False(dto.HasPreviousYear);
        Assert.Null(dto.IncomeChangeAbs);
        Assert.Null(dto.IncomeChangePct);
        Assert.Null(dto.ExpenseChangeAbs);
        Assert.Null(dto.ExpenseChangePct);
        Assert.Null(dto.NetChangeAbs);
        Assert.Null(dto.NetChangePct);
        Assert.Null(dto.SavingsChangeAbs);
        Assert.Null(dto.SavingsChangePct);
        Assert.Null(dto.YearRank);
        Assert.Equal(1, dto.TotalYears);
    }
}

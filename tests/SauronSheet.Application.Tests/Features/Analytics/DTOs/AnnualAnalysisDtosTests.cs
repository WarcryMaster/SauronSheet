namespace SauronSheet.Application.Tests.Features.Analytics.DTOs;

using System;
using Xunit;

using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Tests for DTOs used in the annual analysis feature.
/// Verifies construction, null semantics, and computed properties.
/// </summary>
public class AnnualAnalysisDtosTests
{
    // ---- Task 1.1: YearOverYearVariationDto construction & null semantics ----

    [Fact]
    [Trait("Category", "Application")]
    public void YearOverYearVariationDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange & Act
        YearOverYearVariationDto dto = new(
            IncomeFixedPct: 10.5m,
            IncomeVariablePct: 5.0m,
            IncomeTotalPct: 8.2m,
            ExpenseFixedPct: -3.1m,
            ExpenseVariablePct: 2.0m,
            ExpenseTotalPct: -1.5m,
            NetPct: 12.3m,
            HasPreviousYearData: true);

        // Assert
        Assert.Equal(10.5m, dto.IncomeFixedPct);
        Assert.Equal(5.0m, dto.IncomeVariablePct);
        Assert.Equal(8.2m, dto.IncomeTotalPct);
        Assert.Equal(-3.1m, dto.ExpenseFixedPct);
        Assert.Equal(2.0m, dto.ExpenseVariablePct);
        Assert.Equal(-1.5m, dto.ExpenseTotalPct);
        Assert.Equal(12.3m, dto.NetPct);
        Assert.True(dto.HasPreviousYearData);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void YearOverYearVariationDto_NoPreviousYearData_AllFieldsNull()
    {
        // Arrange & Act
        YearOverYearVariationDto dto = new(
            IncomeFixedPct: null,
            IncomeVariablePct: null,
            IncomeTotalPct: null,
            ExpenseFixedPct: null,
            ExpenseVariablePct: null,
            ExpenseTotalPct: null,
            NetPct: null,
            HasPreviousYearData: false);

        // Assert
        Assert.Null(dto.IncomeFixedPct);
        Assert.Null(dto.IncomeVariablePct);
        Assert.Null(dto.IncomeTotalPct);
        Assert.Null(dto.ExpenseFixedPct);
        Assert.Null(dto.ExpenseVariablePct);
        Assert.Null(dto.ExpenseTotalPct);
        Assert.Null(dto.NetPct);
        Assert.False(dto.HasPreviousYearData);
    }

    // ---- Task 1.3: MonthsWithData defaults to 0, Variation null on Summary ----

    [Fact]
    [Trait("Category", "Application")]
    public void AnnualAnalysisSummaryDto_Default_NewInstanceHasDefaultValues()
    {
        // Arrange & Act
        AnnualAnalysisSummaryDto dto = new(
            100m, 200m, 300m, 50m, 150m, 200m, 100m, "EUR");

        // Assert
        // MonthsWithData should default to 0 (init-only, not set → 0)
        Assert.Equal(0, dto.MonthsWithData);
        // Variation should default to null (init-only, not set → null)
        Assert.Null(dto.Variation);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void AnnualAnalysisSummaryDto_WithMonthsAndVariation_ValuesAssigned()
    {
        // Arrange
        YearOverYearVariationDto variation = new(
            10m, null, 10m, -5m, null, -5m, 15m, true);

        // Act
        AnnualAnalysisSummaryDto dto = new(
            100m, 200m, 300m, 50m, 150m, 200m, 100m, "EUR")
        {
            MonthsWithData = 12,
            Variation = variation
        };

        // Assert
        Assert.Equal(12, dto.MonthsWithData);
        Assert.NotNull(dto.Variation);
        Assert.Equal(10m, dto.Variation.IncomeFixedPct);
        Assert.True(dto.Variation.HasPreviousYearData);
    }

    // ---- Task 1.5: IsIncome computed property on Row ----

    [Fact]
    [Trait("Category", "Application")]
    public void AnnualAnalysisRowDto_IncomeFixed_IsIncomeIsTrue()
    {
        // Arrange & Act
        AnnualAnalysisRowDto dto = new(
            Movement: "Nómina",
            LineType: AnalysisLineType.IncomeFixed,
            TypeLabel: "Ingreso Fijo",
            Average: 1500m,
            MonthlyAmounts: new[] { 1500m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m },
            Currency: "EUR");

        // Assert
        Assert.True(dto.IsIncome);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void AnnualAnalysisRowDto_IncomeVariable_IsIncomeIsTrue()
    {
        // Arrange & Act
        AnnualAnalysisRowDto dto = new(
            Movement: "Freelance",
            LineType: AnalysisLineType.IncomeVariable,
            TypeLabel: "Ingreso Variable",
            Average: 500m,
            MonthlyAmounts: new decimal[12],
            Currency: "EUR");

        // Assert
        Assert.True(dto.IsIncome);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void AnnualAnalysisRowDto_ExpenseFixed_IsIncomeIsFalse()
    {
        // Arrange & Act
        AnnualAnalysisRowDto dto = new(
            Movement: "Hipoteca",
            LineType: AnalysisLineType.ExpenseFixed,
            TypeLabel: "Gasto Fijo",
            Average: 800m,
            MonthlyAmounts: new decimal[12],
            Currency: "EUR");

        // Assert
        Assert.False(dto.IsIncome);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void AnnualAnalysisRowDto_ExpenseVariable_IsIncomeIsFalse()
    {
        // Arrange & Act
        AnnualAnalysisRowDto dto = new(
            Movement: "Supermercado",
            LineType: AnalysisLineType.ExpenseVariable,
            TypeLabel: "Gasto Variable",
            Average: 300m,
            MonthlyAmounts: new decimal[12],
            Currency: "EUR");

        // Assert
        Assert.False(dto.IsIncome);
    }
}

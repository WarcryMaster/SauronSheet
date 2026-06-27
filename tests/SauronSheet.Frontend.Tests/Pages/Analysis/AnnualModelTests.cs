using System.Text.Json;
using Moq;
using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Frontend.Pages.Analysis;
using MediatR;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Analysis;

/// <summary>
/// Tests for the /Analysis/Annual AnnualModel computed properties.
/// Covers monthly aggregation, chart JSON serialization, and fixed cost percentage.
/// </summary>
public class AnnualModelTests
{
    private static readonly decimal[] IncomeFixedAmounts = new[]
    {
        100m, 200m, 300m, 400m, 500m, 600m, 700m, 800m, 900m, 1000m, 1100m, 1200m
    };

    private static readonly decimal[] IncomeVariableAmounts = new[]
    {
        50m, 50m, 50m, 50m, 50m, 50m, 50m, 50m, 50m, 50m, 50m, 50m
    };

    private static readonly decimal[] ExpenseFixedAmounts = new[]
    {
        60m, 60m, 60m, 60m, 60m, 60m, 60m, 60m, 60m, 60m, 60m, 60m
    };

    private static readonly decimal[] ExpenseVariableAmounts = new[]
    {
        30m, 40m, 50m, 60m, 70m, 80m, 90m, 100m, 110m, 120m, 130m, 140m
    };

    private static readonly decimal[] ExpectedMonthlyIncomeTotals = new[]
    {
        150m, 250m, 350m, 450m, 550m, 650m, 750m, 850m, 950m, 1050m, 1150m, 1250m
    };

    private static readonly decimal[] ExpectedMonthlyExpenseTotals = new[]
    {
        90m, 100m, 110m, 120m, 130m, 140m, 150m, 160m, 170m, 180m, 190m, 200m
    };

    [Fact]
    [Trait("Category", "Frontend")]
    public void MonthlyIncomeTotals_WithData_ReturnsAggregatedIncomePerMonth()
    {
        // Arrange
        AnnualModel model = CreateModelWithData();

        // Act
        decimal[] actual = model.MonthlyIncomeTotals;

        // Assert
        Assert.Equal(ExpectedMonthlyIncomeTotals, actual);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void MonthlyIncomeTotals_NoData_ReturnsTwelveZeros()
    {
        // Arrange
        AnnualModel model = CreateEmptyModel();

        // Act
        decimal[] actual = model.MonthlyIncomeTotals;

        // Assert
        Assert.Equal(12, actual.Length);
        Assert.All(actual, value => Assert.Equal(0m, value));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void MonthlyExpenseTotals_WithData_ReturnsAggregatedExpensePerMonth()
    {
        // Arrange
        AnnualModel model = CreateModelWithData();

        // Act
        decimal[] actual = model.MonthlyExpenseTotals;

        // Assert
        Assert.Equal(ExpectedMonthlyExpenseTotals, actual);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void MonthlyExpenseTotals_NoData_ReturnsTwelveZeros()
    {
        // Arrange
        AnnualModel model = CreateEmptyModel();

        // Act
        decimal[] actual = model.MonthlyExpenseTotals;

        // Assert
        Assert.Equal(12, actual.Length);
        Assert.All(actual, value => Assert.Equal(0m, value));
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void ChartDataJson_NoData_ReturnsEmptyObject()
    {
        // Arrange
        AnnualModel model = CreateEmptyModel();

        // Act
        string json = model.ChartDataJson;

        // Assert
        Assert.Equal("{}", json);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void ChartDataJson_WithData_ReturnsExpectedStructureAndValues()
    {
        // Arrange
        AnnualModel model = CreateModelWithData();

        // Act
        string json = model.ChartDataJson;
        JsonDocument document = JsonDocument.Parse(json);

        // Assert
        JsonElement root = document.RootElement;
        string[] labels = root.GetProperty("labels").EnumerateArray().Select(e => e.GetString()!).ToArray();
        decimal[] income = root.GetProperty("income").EnumerateArray().Select(e => e.GetDecimal()).ToArray();
        decimal[] expense = root.GetProperty("expense").EnumerateArray().Select(e => e.GetDecimal()).ToArray();

        Assert.Equal(new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" }, labels);
        Assert.Equal(ExpectedMonthlyIncomeTotals, income);
        Assert.Equal(ExpectedMonthlyExpenseTotals, expense);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void FixedVariableChartJson_NoData_ReturnsEmptyObject()
    {
        // Arrange
        AnnualModel model = CreateEmptyModel();

        // Act
        string json = model.FixedVariableChartJson;

        // Assert
        Assert.Equal("{}", json);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void FixedVariableChartJson_WithData_ReturnsExpectedStructureAndValues()
    {
        // Arrange
        AnnualModel model = CreateModelWithData();

        // Act
        string json = model.FixedVariableChartJson;
        JsonDocument document = JsonDocument.Parse(json);

        // Assert
        JsonElement root = document.RootElement;
        string[] labels = root.GetProperty("labels").EnumerateArray().Select(e => e.GetString()!).ToArray();
        decimal[] values = root.GetProperty("values").EnumerateArray().Select(e => e.GetDecimal()).ToArray();

        Assert.Equal(new[] { "Ingreso Fijo", "Ingreso Variable", "Gasto Fijo", "Gasto Variable" }, labels);
        Assert.Equal(new[] { 7800m, 600m, 720m, 1020m }, values);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void FixedCostPercentage_NoData_ReturnsZero()
    {
        // Arrange
        AnnualModel model = CreateEmptyModel();

        // Act
        decimal actual = model.FixedCostPercentage;

        // Assert
        Assert.Equal(0m, actual);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void FixedCostPercentage_ZeroExpenseTotal_ReturnsZero()
    {
        // Arrange
        AnnualModel model = CreateModelWithZeroExpenseTotal();

        // Act
        decimal actual = model.FixedCostPercentage;

        // Assert
        Assert.Equal(0m, actual);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public void FixedCostPercentage_WithData_ReturnsRoundedFixedCostRatio()
    {
        // Arrange
        AnnualModel model = CreateModelWithData();

        // Act
        decimal actual = model.FixedCostPercentage;

        // Assert
        // ExpenseFixed = 720, ExpenseTotal = 720 + 1020 = 1740
        // 720 / 1740 * 100 = 41.37931... => rounded to 41.4
        Assert.Equal(41.4m, actual);
    }

    private static AnnualModel CreateModelWithData()
    {
        AnnualAnalysisResultDto result = CreateSampleResult();
        AnnualModel model = new AnnualModel(Mock.Of<IMediator>())
        {
            Result = result
        };
        return model;
    }

    private static AnnualModel CreateEmptyModel()
    {
        return new AnnualModel(Mock.Of<IMediator>());
    }

    private static AnnualModel CreateModelWithZeroExpenseTotal()
    {
        AnnualAnalysisSummaryDto summary = new AnnualAnalysisSummaryDto(
            IncomeFixed: 100m,
            IncomeVariable: 50m,
            IncomeTotal: 150m,
            ExpenseFixed: 0m,
            ExpenseVariable: 0m,
            ExpenseTotal: 0m,
            Net: 150m,
            Currency: "EUR");

        AnnualAnalysisResultDto result = new AnnualAnalysisResultDto(
            Year: 2026,
            Rows: Array.Empty<AnnualAnalysisRowDto>(),
            Summary: summary,
            HasData: true,
            Currency: "EUR");

        return new AnnualModel(Mock.Of<IMediator>())
        {
            Result = result
        };
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
                MonthlyAmounts: IncomeFixedAmounts,
                Currency: "EUR"),
            new AnnualAnalysisRowDto(
                Movement: "Freelance",
                LineType: AnalysisLineType.IncomeVariable,
                TypeLabel: "Ingreso Variable",
                Average: 50m,
                MonthlyAmounts: IncomeVariableAmounts,
                Currency: "EUR"),
            new AnnualAnalysisRowDto(
                Movement: "Rent",
                LineType: AnalysisLineType.ExpenseFixed,
                TypeLabel: "Gasto Fijo",
                Average: 60m,
                MonthlyAmounts: ExpenseFixedAmounts,
                Currency: "EUR"),
            new AnnualAnalysisRowDto(
                Movement: "Food",
                LineType: AnalysisLineType.ExpenseVariable,
                TypeLabel: "Gasto Variable",
                Average: 85m,
                MonthlyAmounts: ExpenseVariableAmounts,
                Currency: "EUR")
        };

        AnnualAnalysisSummaryDto summary = new AnnualAnalysisSummaryDto(
            IncomeFixed: 7800m,
            IncomeVariable: 600m,
            IncomeTotal: 8400m,
            ExpenseFixed: 720m,
            ExpenseVariable: 1020m,
            ExpenseTotal: 1740m,
            Net: 6660m,
            Currency: "EUR");

        return new AnnualAnalysisResultDto(
            Year: 2026,
            Rows: rows,
            Summary: summary,
            HasData: true,
            Currency: "EUR");
    }
}

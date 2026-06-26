namespace SauronSheet.Application.Tests.Features.Analytics.Classification;

using System;
using System.Collections.Generic;
using System.Linq;
using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using Xunit;

/// <summary>
/// Unit tests for AnnualClassificationEngine.
/// Covers normalization, static mapping, heuristic, null handling and sign classification.
/// </summary>
public class AnnualClassificationEngineTests
{
    private readonly AnnualClassificationEngine _engine = new();
    private const string Currency = "EUR";

    [Fact]
    [Trait("Category", "Application")]
    public void Normalize_CollapsesDiacriticsAndSpaces()
    {
        // Arrange
        const string input = "Cafeterías  y\nRestaurantes";
        const string expected = "cafeterias y restaurantes";

        // Act
        string actual = AnnualClassificationEngine.Normalize(input);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(AnalysisLineType.ExpenseFixed, "Gasto Fijo")]
    [InlineData(AnalysisLineType.ExpenseVariable, "Gasto Variable")]
    [InlineData(AnalysisLineType.IncomeFixed, "Ingreso Fijo")]
    [InlineData(AnalysisLineType.IncomeVariable, "Ingreso Variable")]
    [Trait("Category", "Application")]
    public void AnalysisLineType_GetTypeLabel_ReturnsSpanishLabels(
        AnalysisLineType lineType,
        string expected)
    {
        // Act
        string actual = lineType.GetTypeLabel();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_StaticMapping_ExpenseFixed()
    {
        // Arrange
        SubcategoryId subcategoryId = SubcategoryId.New();
        Dictionary<SubcategoryId, string> names = new()
        {
            [subcategoryId] = "Luz y gas"
        };

        List<Transaction> transactions = new()
        {
            CreateTransaction(-50m, new DateTime(2026, 1, 15), subcategoryId, "Iberdrola")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Single(rows);
        Assert.Equal("Luz y gas", rows[0].Movement);
        Assert.Equal(AnalysisLineType.ExpenseFixed, rows[0].LineType);
        Assert.Equal("Gasto Fijo", rows[0].TypeLabel);
        Assert.Equal(50m / 12, rows[0].Average);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_StaticMapping_ExpenseVariable()
    {
        // Arrange
        SubcategoryId subcategoryId = SubcategoryId.New();
        Dictionary<SubcategoryId, string> names = new()
        {
            [subcategoryId] = "Supermercados y alimentación"
        };

        List<Transaction> transactions = new()
        {
            CreateTransaction(-75.50m, new DateTime(2026, 2, 10), subcategoryId, "Carrefour")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Single(rows);
        Assert.Equal("Supermercados y alimentación", rows[0].Movement);
        Assert.Equal(AnalysisLineType.ExpenseVariable, rows[0].LineType);
        Assert.Equal("Gasto Variable", rows[0].TypeLabel);
        Assert.Equal(75.50m / 12, rows[0].Average);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_NullSubcategory_ReturnsSinClasificar()
    {
        // Arrange
        Dictionary<SubcategoryId, string> names = new();
        List<Transaction> transactions = new()
        {
            CreateTransaction(-20m, new DateTime(2026, 3, 5), null, "Retirada cajero")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Single(rows);
        Assert.Equal("Sin clasificar", rows[0].Movement);
        Assert.Equal(AnalysisLineType.ExpenseVariable, rows[0].LineType);
        Assert.Equal("Gasto Variable", rows[0].TypeLabel);
        Assert.Equal(20m / 12, rows[0].Average);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_IncomeOtherEntities_ReturnsIncomeVariable()
    {
        // Arrange
        SubcategoryId subcategoryId = SubcategoryId.New();
        Dictionary<SubcategoryId, string> names = new()
        {
            [subcategoryId] = "Ingresos de otras entidades"
        };

        List<Transaction> transactions = new()
        {
            CreateTransaction(250m, new DateTime(2026, 1, 20), subcategoryId, "Bizum amigo")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Single(rows);
        Assert.Equal("Ingresos de otras entidades", rows[0].Movement);
        Assert.Equal(AnalysisLineType.IncomeVariable, rows[0].LineType);
        Assert.Equal("Ingreso Variable", rows[0].TypeLabel);
        Assert.Equal(250m / 12, rows[0].Average);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_IncomeOther_ReturnsIncomeFixed()
    {
        // Arrange
        SubcategoryId subcategoryId = SubcategoryId.New();
        Dictionary<SubcategoryId, string> names = new()
        {
            [subcategoryId] = "Nómina"
        };

        List<Transaction> transactions = new()
        {
            CreateTransaction(1500m, new DateTime(2026, 1, 28), subcategoryId, "Nómina enero")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Single(rows);
        Assert.Equal("Nómina", rows[0].Movement);
        Assert.Equal(AnalysisLineType.IncomeFixed, rows[0].LineType);
        Assert.Equal("Ingreso Fijo", rows[0].TypeLabel);
        Assert.Equal(1500m / 12, rows[0].Average);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_RecurrenceHeuristic_CvLe10_3PlusMonths_ReturnsFixed()
    {
        // Arrange
        SubcategoryId subcategoryId = SubcategoryId.New();
        Dictionary<SubcategoryId, string> names = new()
        {
            [subcategoryId] = "Gimnasio"
        };

        List<Transaction> transactions = new()
        {
            CreateTransaction(-30m, new DateTime(2026, 1, 10), subcategoryId, "Cuota gimnasio"),
            CreateTransaction(-31m, new DateTime(2026, 2, 10), subcategoryId, "Cuota gimnasio"),
            CreateTransaction(-30m, new DateTime(2026, 3, 10), subcategoryId, "Cuota gimnasio"),
            CreateTransaction(-31m, new DateTime(2026, 4, 10), subcategoryId, "Cuota gimnasio")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Single(rows);
        Assert.Equal("Gimnasio", rows[0].Movement);
        Assert.Equal(AnalysisLineType.ExpenseFixed, rows[0].LineType);
        Assert.Equal("Gasto Fijo", rows[0].TypeLabel);
        Assert.Equal(122m / 12, rows[0].Average);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_RecurrenceHeuristic_CvGt10_ReturnsVariable()
    {
        // Arrange
        SubcategoryId subcategoryId = SubcategoryId.New();
        Dictionary<SubcategoryId, string> names = new()
        {
            [subcategoryId] = "Viaje"
        };

        List<Transaction> transactions = new()
        {
            CreateTransaction(-10m, new DateTime(2026, 1, 5), subcategoryId, "Gastos viaje"),
            CreateTransaction(-20m, new DateTime(2026, 2, 5), subcategoryId, "Gastos viaje"),
            CreateTransaction(-30m, new DateTime(2026, 3, 5), subcategoryId, "Gastos viaje"),
            CreateTransaction(-40m, new DateTime(2026, 4, 5), subcategoryId, "Gastos viaje")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Single(rows);
        Assert.Equal("Viaje", rows[0].Movement);
        Assert.Equal(AnalysisLineType.ExpenseVariable, rows[0].LineType);
        Assert.Equal("Gasto Variable", rows[0].TypeLabel);
        Assert.Equal(100m / 12, rows[0].Average);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_AmountZero_Excluded()
    {
        // Arrange
        SubcategoryId subcategoryId = SubcategoryId.New();
        Dictionary<SubcategoryId, string> names = new()
        {
            [subcategoryId] = "Nómina"
        };

        List<Transaction> transactions = new()
        {
            CreateTransaction(0m, new DateTime(2026, 1, 15), subcategoryId, "Ajuste")
        };

        // Act
        IReadOnlyList<AnnualAnalysisRowDto> rows = _engine.Classify(transactions, names, 2026);

        // Assert
        Assert.Empty(rows);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void Classify_GuardMeanZero_ReturnsVariable()
    {
        // The CV guard mean > 0 must return false (Variable) when the mean is zero.
        List<decimal> amounts = new() { 0m, 0m, 0m };

        // Act
        bool isFixed = AnnualClassificationEngine.IsRecurrentFixed(amounts, distinctMonths: 3);

        // Assert
        Assert.False(isFixed);
    }

    private static Transaction CreateTransaction(
        decimal amount,
        DateTime date,
        SubcategoryId? subcategoryId,
        string description)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, Currency),
            date,
            description,
            subcategoryId: subcategoryId);
    }
}
